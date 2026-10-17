using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimPosition : MonoBehaviour
{
    private static AimPosition instance;
    // Start is called before the first frame update
    [SerializeField] private Camera mainCamera;
    public LayerMask Ignorelayer;
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray,out RaycastHit hit,Mathf.Infinity,~Ignorelayer))
        {
            transform.position = hit.point;  
        }
        else
        {
            transform.position = mainCamera.ScreenToWorldPoint(Input.mousePosition+new Vector3(0,0,100));
        }
        //Debug.Log(mainCamera.ScreenToWorldPoint(Input.mousePosition));
        //Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
