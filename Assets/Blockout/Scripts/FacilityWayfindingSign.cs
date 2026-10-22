using TMPro;
using UnityEngine;

namespace Blockout.Discovery
{
    [ExecuteAlways]
    public class FacilityWayfindingSign : MonoBehaviour
    {
        public enum ArrowDirection { Left, Right, Ahead, Back, UpLeft, UpRight, DownLeft, DownRight, Hidden }
        [TextArea] public string Label="STORAGE ROOM";
        public string ZoneCode="01 / STORAGE";
        public Color RouteColor=Color.yellow;
        public ArrowDirection Direction=ArrowDirection.Left;
        public TMP_Text LabelText,CodeText;
        public Transform Arrow;
        public Renderer ColorBand,ArrowRenderer;
        MaterialPropertyBlock block;
        void OnEnable()=>Refresh();
        void OnValidate()=>Refresh();
        public void Refresh()
        {
            if(LabelText)LabelText.text=Label;
            if(CodeText)CodeText.text=ZoneCode;
            if(Arrow)
            {
                Arrow.gameObject.SetActive(Direction!=ArrowDirection.Hidden);
                float[] angles={180,0,90,-90,135,45,-135,-45,0};
                Arrow.localRotation=Quaternion.Euler(0,0,angles[(int)Direction]);
            }
            block??=new MaterialPropertyBlock();
            block.SetColor("_BaseColor",RouteColor);block.SetColor("_Color",RouteColor);
            foreach(var renderer in new[]{ColorBand,ArrowRenderer})if(renderer)renderer.SetPropertyBlock(block);
        }
    }
}
