using Spine;
using Spine.Unity;
using UnityEngine;

public class CombineSkin : MonoBehaviour
{
    [SpineSkin]
    public string[] combinedSkins = { };
    [SerializeField] SkeletonDataAsset blueHairAsset;
    [SerializeField] SkeletonDataAsset redHairAsset;
    private SkeletonMecanim spineObject;
    private Skin combinedSkin;
    private string hairColor = "red";

    void Start()
    {
        spineObject = GetComponent<SkeletonMecanim>();
        combinedSkin = new Skin(name: "combinedSkin");
    }

    void Update() {
        Refresh();
    }

    public void Refresh()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        spineObject = GetComponent<SkeletonMecanim>();
        combinedSkin = new Skin(name: "combinedSkin");
        if (combinedSkin != null && spineObject != null)
        {
            for (int i = 0; i < combinedSkins.Length; i++)
            {
                if (combinedSkins[i] == "Hair/Blue Hair")
                {
                    hairColor = "blue";
                }
                else if (combinedSkins[i] == "Hair/Red Hair")
                {
                    hairColor = "red";
                }
                var skinName = combinedSkins[i];
                combinedSkin.AddSkin(spineObject.skeleton.Data.FindSkin(skinName));
            }

            if (hairColor == "red")
            {
                spineObject.skeletonDataAsset = redHairAsset;
            }
            else if (hairColor == "blue")
            {
                spineObject.skeletonDataAsset = blueHairAsset;
            }
            spineObject.skeleton.SetSkin(combinedSkin);
            spineObject.skeleton.SetSlotsToSetupPose();
        }
    }
}
