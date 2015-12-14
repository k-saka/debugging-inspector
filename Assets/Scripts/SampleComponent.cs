using UnityEngine;

[Inspectable]
public class SampleComponent : MonoBehaviour
{
    [Inspectable]
    int sampleInt;

    [Inspectable]
    float sampleFloat;

    [Inspectable]
    public bool SampleBoolean { get; private set; }

    [Inspectable]
    Vector3 SampleVector3 { get { return transform.position; } }

    [Inspectable]
    Color SampleColor { get { return Color.black; } }

    [Inspectable]
    SampleType SampleType { get; set; }

    int ignoreInspection = 1;

    void Start()
    {
        sampleInt = 42;
        sampleFloat = 0f;
        SampleBoolean = true;
        SampleType = new SampleType();
    }

    void Update()
    {
        sampleFloat += Time.deltaTime;
    }
}
