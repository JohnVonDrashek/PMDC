using System;
using PMDC.Data;
using RogueEssence.Data;
using RogueEssence.Dev.ViewModels;

namespace PMDC.Dev.ViewModels
{
    /// <summary>
    /// View model for displaying monster form data in the team member spawn editor.
    /// Provides bindable properties for monster stats, types, abilities, and form information.
    /// </summary>
    public class BaseMonsterFormViewModel : ViewModelBase
    {
        /// <summary>
        /// The underlying monster form data being wrapped.
        /// </summary>
        private MonsterFormData monsterForm;

        /// <summary>
        /// Gets the index of this form in the filtered list for selection tracking.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Creates a new BaseMonsterFormViewModel for the specified monster form.
        /// </summary>
        /// <param name="key">The species key (e.g., "pikachu").</param>
        /// <param name="formId">The form index within the species.</param>
        /// <param name="index">The index in the overall form list.</param>
        public BaseMonsterFormViewModel(string key, int formId, int index)
        {
            MonsterData data = DataManager.Instance.GetMonster(key);
            monsterForm = (MonsterFormData) data.Forms[formId];
            
            this.key = key;
            this.formId = formId;
            this.joinRate = data.JoinRate;
            
            Index = index;
        }
        
        /// <summary>
        /// Gets the localized display name of this monster form.
        /// </summary>
        public string Name
        {
            get { return monsterForm.FormName.ToLocal();  }
        }

        private string key;

        /// <summary>
        /// Gets the species key identifier.
        /// </summary>
        public string Key
        {
            get { return key; }
        }

        private int formId;

        /// <summary>
        /// Gets the form index within the species.
        /// </summary>
        public int FormId
        {
            get { return formId; }
        }

        /// <summary>
        /// Gets the localized display name of the primary element.
        /// </summary>
        public string Element1Display
        {
            get { return DataManager.Instance.GetElement(monsterForm.Element1).Name.ToLocal(); }
        }

        /// <summary>
        /// Gets the localized display name of the secondary element.
        /// </summary>
        public string Element2Display
        {
            get { return DataManager.Instance.GetElement(monsterForm.Element2).Name.ToLocal(); }
        }

        /// <summary>
        /// Gets the primary element key.
        /// </summary>
        public string Element1
        {
            get { return monsterForm.Element1; }
        }

        /// <summary>
        /// Gets the secondary element key.
        /// </summary>
        public string Element2
        {
            get { return monsterForm.Element2;  }
        }

        /// <summary>
        /// Gets the localized name of the first ability.
        /// </summary>
        public string Intrinsic1
        {
            get { return DataManager.Instance.GetIntrinsic(monsterForm.Intrinsic1).Name.ToLocal(); }
        }

        /// <summary>
        /// Gets the localized name of the second ability.
        /// </summary>
        public string Intrinsic2
        {
            get
            {
                return DataManager.Instance.GetIntrinsic(monsterForm.Intrinsic2).Name.ToLocal();
            }
        }

        /// <summary>
        /// Gets the localized name of the hidden (third) ability.
        /// </summary>
        public string Intrinsic3
        {
            get { return DataManager.Instance.GetIntrinsic(monsterForm.Intrinsic3).Name.ToLocal(); }
        }

        /// <summary>
        /// Gets whether this is a temporary form (e.g., mega evolution, alternate appearance).
        /// </summary>
        public bool Temporary
        {
            get { return monsterForm.Temporary; }
        }

        /// <summary>
        /// Gets whether this form has been released for use in the game.
        /// </summary>
        public bool Released
        {
            get { return monsterForm.Released; }
        }

        /// <summary>
        /// Gets the base HP stat.
        /// </summary>
        public int BaseHP
        {
            get { return monsterForm.BaseHP; }
        }

        /// <summary>
        /// Gets the base Attack stat.
        /// </summary>
        public int BaseAtk
        {
            get { return monsterForm.BaseAtk; }
        }

        /// <summary>
        /// Gets the base Defense stat.
        /// </summary>
        public int BaseDef
        {
            get { return monsterForm.BaseDef; }
        }

        /// <summary>
        /// Gets the base Special Attack stat.
        /// </summary>
        public int BaseMAtk
        {
            get { return monsterForm.BaseMAtk; }
        }

        /// <summary>
        /// Gets the base Special Defense stat.
        /// </summary>
        public int BaseMDef
        {
            get { return monsterForm.BaseMDef; }
        }

        /// <summary>
        /// Gets the base Speed stat.
        /// </summary>
        public int BaseSpeed
        {
            get { return monsterForm.BaseSpeed; }
        }

        /// <summary>
        /// Gets the total of all base stats (base stat total).
        /// </summary>
        public int BaseTotal
        {
            get
            {
                return monsterForm.BaseHP + monsterForm.BaseAtk + monsterForm.BaseDef +
                     monsterForm.BaseMAtk +  monsterForm.BaseMDef +  monsterForm.BaseSpeed;
            }
        }

        private int joinRate;

        /// <summary>
        /// Gets the base recruitment rate for this species.
        /// </summary>
        public int JoinRate
        {
            get
            {
                return joinRate;
            }
        }

        /// <summary>
        /// Gets the experience points yielded when this form is defeated.
        /// </summary>
        public int ExpYield
        {
            get
            {
                return monsterForm.ExpYield;
            }
        }
    }
}