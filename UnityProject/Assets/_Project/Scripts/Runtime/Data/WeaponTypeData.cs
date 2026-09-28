using UnityEngine;

namespace BattleFight
{
    /// <summary>武器種ごとの見た目とボーナス。モデルは攻撃A・攻撃B・移動で共用する。</summary>
    [CreateAssetMenu(menuName = "BattleFight/Weapon Type Data", fileName = "Weapon_")]
    public class WeaponTypeData : ScriptableObject
    {
        public WeaponType weapon;
        public string displayName;
        public Color color = Color.white;

        [Header("見た目(仮モデル)")]
        public PrimitiveType headShape = PrimitiveType.Cube;
        public Vector3 headScale = new Vector3(0.08f, 0.08f, 1.2f);
        public Vector3 headOffset = new Vector3(0f, 0f, 0.7f);
        public Vector3 headRotation;
        [Tooltip("0より大きいと柄を描く")]
        public float handleLength;

        [Header("ボーナス")]
        [Tooltip("3スロットがすべてこの武器種のとき使えるフィニッシャー")]
        public SkillData masteryFinisher;
        [Tooltip("シナジー/マスタリー時の、この武器種の技のひるみ値ボーナス")]
        [Range(0f, 1f)] public float synergyStaggerBonus = 0.1f;
    }
}
