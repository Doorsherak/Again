using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OldManZombie_Customization : MonoBehaviour
{
    // Start is called before the first frame update

    private int bodyTyp;
    private int pantsTyp;
    private int eyesTyp;
    private int susTyp;
    public GameObject partsParent;
    public GameObject[] pantsObjects;
    public GameObject suspendersObject;
    public Material[] BodyMaterials = new Material[2];
    //public Material[] HairMaterials = new Material[1];
    public Material[] ClothesMaterials = new Material[2];


    public enum BodyType
    {
        V1,
        V2
    }

    public enum PantsType
    {
        V1,
        V2
    }
    public enum EyesGlow
    {
        No,
        Yes
    }

    public enum SusPenders
    {
        Yes,
        No
    }


    public BodyType bodyType;
    public PantsType pantsType;
    public SusPenders suspendersType;
    public EyesGlow eyesGlow;
    


    public void charCustomize(int body, int pants, int eyes, int sus)
    {
        Material[] mat;
        if (partsParent != null)
        {
            Renderer[] childRenderers = partsParent.GetComponentsInChildren<Renderer>();

            if (eyes == 0)
            {



                BodyMaterials[body].DisableKeyword("_EMISSION");
                BodyMaterials[body].SetFloat("_EmissiveExposureWeight", 1);
            }
            else
            {


                BodyMaterials[body].EnableKeyword("_EMISSION");
                BodyMaterials[body].SetFloat("_EmissiveExposureWeight", 0);

            }


            foreach (Renderer renderer in childRenderers)
            {


                Material[] materials = renderer.sharedMaterials;




                
                if (materials.Length > 1)
                {

                    //print(materials.Length);
                    mat = new Material[2];
                    mat[0] = BodyMaterials[body];
                    mat[1] = ClothesMaterials[pants];




                    renderer.materials = mat;
                }
                else
                {
                    renderer.sharedMaterial = BodyMaterials[body];
                }

                if (sus == 1)
                {
                    suspendersObject.SetActive(false);
                }
                else
                {
                    suspendersObject.SetActive(true);
                    suspendersObject.GetComponent<Renderer>().sharedMaterial = ClothesMaterials[pants];
                }





            }
        }

        if (pantsObjects[0] != null)
        {
            //print("Pants");

            foreach (GameObject obj in pantsObjects)
            {
                
                Renderer renderer = obj.GetComponent<Renderer>();
                renderer.sharedMaterial = ClothesMaterials[pants];

            }
        }

    }

    void OnValidate()
    {
        //code for In Editor customize

        bodyTyp = (int)bodyType;
        pantsTyp = (int)pantsType;
        eyesTyp = (int)eyesGlow;
        susTyp = (int)suspendersType;

        charCustomize(bodyTyp, pantsTyp, eyesTyp, susTyp);

    }
}
