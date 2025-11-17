using TMPro;
using UnityEngine;

public class ProxyHandler : MonoBehaviour
{
    public PostRunManager.EligibleObject objectType;
    private int decorationNumber = 0;
    public TextMeshPro signText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateDecorationNumber()
    {
        decorationNumber++;
        signText.text = "x " + decorationNumber;
    }

    public void ResetDecorationNumber()
    {
        decorationNumber = 0;
        signText.text = "x " + decorationNumber;
    }
}
