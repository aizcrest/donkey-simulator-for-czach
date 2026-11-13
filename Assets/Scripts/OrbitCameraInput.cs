using UnityEngine;
using UnityEngine.EventSystems;

namespace DonkeyForest
{
    public sealed class OrbitCameraInput:MonoBehaviour
    {
        ForestGame game;
        float yaw,pitch=31;
        int lookFinger=-1;
        bool mouseDragging;
        Vector2 previousMouse;
        public void Setup(ForestGame owner){game=owner;}
        public Vector3 Offset(float distance)=>Quaternion.Euler(pitch,yaw,0)*Vector3.back*distance;
        bool OverUI(int pointer=-1)=>EventSystem.current && EventSystem.current.IsPointerOverGameObject(pointer);
        public void ReadInput()
        {
            if(!game||game.Paused||game.Dead){lookFinger=-1;mouseDragging=false;return;}
            if(Input.touchCount>0)
            {
                mouseDragging=false;bool found=false;
                for(int i=0;i<Input.touchCount;i++)
                {
                    Touch touch=Input.GetTouch(i);
                    if(lookFinger<0&&touch.phase==TouchPhase.Began&&touch.position.x>Screen.width*.5f&&!OverUI(touch.fingerId))
                        lookFinger=touch.fingerId;
                    if(touch.fingerId!=lookFinger)continue;
                    found=true;
                    if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)lookFinger=-1;
                    else if(touch.phase==TouchPhase.Moved)Rotate(touch.deltaPosition);
                }
                if(!found)lookFinger=-1;
                return;
            }
            lookFinger=-1;
            if(Application.isMobilePlatform)return;
            Vector2 mouse=Input.mousePosition;
            if(Input.GetMouseButtonDown(1)){mouseDragging=!OverUI();previousMouse=mouse;}
            if(!Input.GetMouseButton(1))mouseDragging=false;
            if(mouseDragging){Rotate(mouse-previousMouse);previousMouse=mouse;}
        }
        void Rotate(Vector2 pixels)
        {
            float sensitivity=180f/Mathf.Max(1,Screen.height);
            yaw=Mathf.Repeat(yaw+pixels.x*sensitivity,360);
            pitch=Mathf.Clamp(pitch-pixels.y*sensitivity,15,65);
        }
        void OnApplicationFocus(bool focused){if(!focused){lookFinger=-1;mouseDragging=false;}}
    }
}
