using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SexyTimeStarter : MonoBehaviour
{
    [SerializeField] private GameObject sexyTimeLogicObject;

    [Tooltip("The key to press to start Sexy Time")]
    [SerializeField] private KeyCode startKey = KeyCode.L;

    void Update()
    {
        if (Input.GetKeyDown(startKey))
        {
            if (!sexyTimeLogicObject.activeInHierarchy)
            {
                sexyTimeLogicObject.SetActive(true);

                // Optional: explicitly start it (if needed)
                SexyTimeLogic logic = sexyTimeLogicObject.GetComponent<SexyTimeLogic>();
                if (logic != null && !SexyTimeLogic.isSexyTimeGoingOn)
                {
                    logic.StartSexyTime();
                }
            }
        }
    }
}
