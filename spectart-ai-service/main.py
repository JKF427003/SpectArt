#.venv\Scripts\activate
import os, base64, hashlib, json
import traceback
from pathlib import Path
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from dotenv import load_dotenv
from openai import OpenAI

load_dotenv()
api_key = os.getenv("OPENAI_API_KEY")
if not api_key:
    raise RuntimeError("OPENAI_API_KEY missing. Put it in .env")
client = OpenAI(api_key=api_key)

app = FastAPI(title="SpectArt AI Service", version="0.1.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],          
    allow_credentials=False,      
    allow_methods=["*"],
    allow_headers=["*"],
)

CACHE_DIR = Path(__file__).parent / "cache"
IMG_DIR = CACHE_DIR / "images"
RID_DIR = CACHE_DIR / "riddles"
IMG_DIR.mkdir(parents=True, exist_ok=True)
RID_DIR.mkdir(parents=True, exist_ok=True)

app.mount("/cache", StaticFiles(directory=str(CACHE_DIR), html=False), name="cache")

class BundleIn(BaseModel):
    subject: str
    style: str
    lore: str
    size: str = "512x512"           
    force_regen: bool = False

ALLOWED_SIZES = {"256x256", "512x512", "1024x1024"}

def _normalize_size(size_str: str) -> str:
    valid = ["1024x1024", "1024x1536", "1536x1024", "auto"]
    if size_str in valid:
        return size_str
    return "1024x1024"

def _key_for(subject: str, style: str, lore: str, size_str:str) -> str:
    norm_size = _normalize_size(size_str)
    h = hashlib.sha256(f"{subject}|{style}|{lore}|{norm_size}".encode("utf-8")).hexdigest()
    return h[:16]

def _riddle_prompt(subject, style, lore):
    return (
        "You are a museum riddle curator.\n"
        "Return STRICT JSON with fields:\n"
        " - riddle (2 short lines)\n"
        " - answer (canonical short noun phrase)\n"
        " - acceptable_answers (array of 2-5 strings including synonyms, lowercase)\n"
        " - hint (one short sentence)\n"
        "No extra keys, no code fences.\n\n"
        f"Artwork: {subject}\n"
        f"Visual style: {style}\n"
        f"Lore to subtly hint: {lore}\n"
        "Tone: eerie but PG-13, clever, solvable.\n"
    )

@app.get("/")
def home():
    return {"app":"SpectArt AI Service","status":"ok","try":["/docs","POST /bundle","/cache/images/<file>.png"]}

@app.get("/favicon.ico")
def favicon():
    return {"ok": True}

@app.get("/debug/cache")
def debug_cache():
    imgs = [p.name for p in IMG_DIR.glob("*.png")]
    rids = [p.name for p in RID_DIR.glob("*.json")]
    return {"images": imgs, "riddles": rids}

@app.post("/bundle")
def bundle(req: BundleIn):
    norm_size = _normalize_size(req.size)
    key = _key_for(req.subject, req.style, req.lore, norm_size)
    img_path = IMG_DIR / f"{key}.png"
    rid_path = RID_DIR / f"{key}.json"

    # ---- cache ----
    if not req.force_regen and img_path.exists() and rid_path.exists():
        with open(rid_path, "r", encoding="utf-8") as f:
            rid_json = json.load(f)
        return {
            "cache_hit": True,
            "image_url": f"/cache/images/{key}.png",
            "riddle": rid_json
        }

    try:
        chat = client.chat.completions.create(
            model = "gpt-4o-mini",
            response_format = {"type": "json_object"},
            messages = [
                {"role": "system", "content": "You output only strict JSON for riddles."},
                {"role": "user", "content": _riddle_prompt(req.subject, req.style, req.lore)},
            ],
            temperature = 0.8,
        )
        content = chat.choices[0].message.content
        rid_json = json.loads(content)
        assert "riddle" in rid_json and "answer" in rid_json and "hint" in rid_json
        if "acceptable_answers" not in rid_json or not isinstance(rid_json["acceptable_answers"], list):
            rid_json["acceptable_answers"] = [rid_json["answer"]]
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Riddle generation failed: {e}")
    
    try:
        with open(rid_path, "w", encoding="utf-8") as f:
            json.dump(rid_json, f, ensure_ascii=False, indent=2)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Riddle cache write failed: {e}")

    try:
        style_preset = "painting, museum wall, gallery lighting, moody, high detail, no text"
        prompt = f"{req.subject}, {req.style}. Render as {style_preset}."

        safe_size = "1024x1024" if norm_size not in ["1024x1024", "1024x1536", "1536x1024", "auto"] else norm_size
        
        img = client.images.generate(
            model="gpt-image-1",
            prompt=prompt,
            size=safe_size,
        )

        b64 = img.data[0].b64_json
        png = base64.b64decode(b64)

        img_path = IMG_DIR / f"{key}.png"
        with open(img_path, "wb") as f:
            f.write(png)

        if not img_path.exists() or img_path.stat().st_size == 0:
            raise RuntimeError("Image file did not save correctly.")

        image_url = f"/cache/images/{img_path.name}"
    except Exception as e:
        traceback.print_exc()  
        image_url = None
        print(f"[WARN] Image generation failed: {e}")  

    return {
        "cache_hit": False,
        "image_url": image_url,
        "riddle": rid_json
    }