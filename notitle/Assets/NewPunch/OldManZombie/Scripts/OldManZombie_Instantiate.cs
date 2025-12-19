using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OldManZombie_Instantiate : MonoBehaviour
{
    // Start is called before the first frame update

    private int bodyTyp;
    private int pantsTyp;
    private int eyesTyp;
    private int susTyp;
    


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

    public Transform prefabObject;
    public BodyType bodyType;
    public PantsType pantsType;
    public SusPenders suspendersType;
    public EyesGlow eyesGlow;
    


    void Start()
    {
        Transform pref = Instantiate(prefabObject, gameObject.transform.position, gameObject.transform.rotation);
        bodyTyp = (int)bodyType;
        pantsTyp = (int)pantsType;
        eyesTyp = (int)eyesGlow;
        susTyp = (int)suspendersType;

        pref.gameObject.GetComponent<OldManZombie_Customization>().charCustomize(bodyTyp, pantsTyp, eyesTyp, susTyp);


    }

    
}
