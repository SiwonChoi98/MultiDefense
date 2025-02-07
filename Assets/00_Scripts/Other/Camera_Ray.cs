using Unity.Services.Vivox;
using UnityEngine;

public class Camera_Ray : MonoBehaviour
{
    private Camera cam;
    private Hero_Holder holder = null;
    private Hero_Holder Move_Holder = null;
    private void Start()
    {
        cam = Camera.main;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MouseButtonDown();
        }

        if (Input.GetMouseButton(0))
        {
            MouseButton();
        }
        
        if(Input.GetMouseButtonUp(0))
        {
            MouseButtonUp();
        }
    }

    private void MouseButtonDown()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (holder != null)
        {
            holder.ReturnRange();
        }
            
        if (hit.collider != null)
        {
            holder = hit.collider.GetComponent<Hero_Holder>();
           
        }
    }
    private void MouseButton()
    {
        if (holder != null)
        {
            holder.G_GetClick(true);
            
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.transform != holder.transform)
            {
                if (Move_Holder != null)
                {
                    Move_Holder.S_SetClick(false);
                }
                Move_Holder = hit.collider.GetComponent<Hero_Holder>();
                Move_Holder.S_SetClick(true);
            }
        }
    }
    
    private void MouseButtonUp()
    {
        if (Move_Holder == null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null)
            {
                if (holder.transform == hit.collider.transform)
                {
                    holder.GetRange();
                }
            }
        }
        else
        {
            Move_Holder.S_SetClick(false);
            
            Spawner.Holder_Position_Set(holder, Move_Holder);
        }

        if (holder != null)
            holder.G_GetClick(false);
    }
}
