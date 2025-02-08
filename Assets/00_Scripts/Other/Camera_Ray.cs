using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public class Camera_Ray : NetworkBehaviour
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
            holder = null;
        }
            
        if (hit.collider != null)
        {
            holder = hit.collider.GetComponent<Hero_Holder>();
            int value = (int)NetworkManager.Singleton.LocalClientId;
            bool CanGet = false;
            if (value == 0) CanGet = holder.Holder_Part_Name.Contains("HOST");
            else if (value == 1) CanGet = holder.Holder_Part_Name.Contains("CLIENT");

            if (!CanGet) holder = null;
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
        if (holder == null) return;
        
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
            
            Spawner.Instance.Holder_Position_Set(holder.Holder_Part_Name, Move_Holder.Holder_Part_Name);
        }

        if (holder != null)
            holder.G_GetClick(false);
        
        Move_Holder = null;
    }
}
