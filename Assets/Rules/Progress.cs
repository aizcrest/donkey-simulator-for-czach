using System;
namespace DonkeyForest
{
    [Serializable] public class Progress
    {
        public int version = 1, kills, coins, sword = 1, armor = 1, barding = 1, potions = 3;
        public float health = 100, time = 0.25f, x, z;
        public int gemMask, gemsPlaced;
        public int gemDepositedMask,scatteredGemMask;
        public float[] gemX=new float[GemRules.Required],gemZ=new float[GemRules.Required];
        public bool dragonDefeated;
        public SkillSave skills=new SkillSave();
        public JourneySave journey=new JourneySave();
        public float masterVolume=.75f,musicVolume=.2f,effectsVolume=.8f;
        public int difficulty; // Normal is zero, including saves made before difficulty selection.
        public int quality; // Low is the default on a fresh or older save.
        public static Progress NewAdventure(Progress previous)
        {
            return new Progress {
                quality=previous.quality,difficulty=previous.difficulty,
                masterVolume=previous.masterVolume,musicVolume=previous.musicVolume,effectsVolume=previous.effectsVolume
            };
        }
    }
}
