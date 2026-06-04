using UnityEngine;

public class MotionLinestoggle : MonoBehaviour
{
   public ParticleSystem CanvasMotion;
   public PlayerInput movement;

   void Update ()
   {
    if (movement.diving == true)
    {
        if(!CanvasMotion.isPlaying)
        {
            CanvasMotion.Play();
        }
    }

    else
    {
        if(CanvasMotion.isPlaying)
        {
            CanvasMotion.Stop();
        }
        
    }
   }
}
