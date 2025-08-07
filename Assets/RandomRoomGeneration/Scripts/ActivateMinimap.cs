using UnityEngine;

public class ActivateMinimap : MonoBehaviour
{

    public GameObject minimap;

    private void Start()
    {
        minimap.SetActive(false);
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            minimap.SetActive(true);
        }
        else if(Input.GetKeyUp(KeyCode.Tab))
        {
            minimap.SetActive(false);
        }
    }
}
