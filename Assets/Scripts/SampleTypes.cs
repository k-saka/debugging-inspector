using UnityEngine;

public enum SampleEnum
{
    Yuno,
    Miyako,
    Sae,
    Hiro
}

[Inspectable]    
public class SampleType
{
    [Inspectable]
    Vector2 SampleVector2 { get; set; }

    [Inspectable]
    public SampleEnum SampleEnum { get; private set; }

    public SampleType()
    {
        SampleVector2 = Vector2.one;
        SampleEnum = SampleEnum.Yuno;
    }
}
