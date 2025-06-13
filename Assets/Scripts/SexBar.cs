using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.TopDownEngine.Common.Scripts
{
    public class SexBar : MonoBehaviour
    {
        public Image fillImage;
        public TextMeshProUGUI gamemenuLevelText;

        public void UpdateBar(float currentXP, float maxXP)
        {
            fillImage.fillAmount = currentXP / maxXP;

            Debug.Log(currentXP);
            //gamemenuLevelText.text = $"Sex level {StatsManager.Instance.sexLevel} Exp {currentXP}/{maxXP}\n     {maxXP - currentXP} exp to next level";
        }
    }
}
