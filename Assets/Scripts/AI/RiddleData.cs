using System;

[Serializable]
public class RiddlePayLoad
{
    public string riddle;
    public string answer;
    public string[] acceptable_answers;
    public string hint;
}

[Serializable]
public class BundleResponse
{
    public bool cache_hit;
    public string image_url;
    public RiddlePayLoad riddle;
}

public class RiddleData
{
    public string Question;
    public string Correct;
    public string[] Acceptable;
    public string Hint;
}
