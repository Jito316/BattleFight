using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>武器種ごとの見た目とボーナス。モデルは攻撃A・攻撃B・移動で共用する。</summary>
    [CreateAssetMenu(menuName = "BattleFight/Weapon Type Data", fileName = "Weapon_")]
    public class WeaponTypeData : ScriptableObject
    {
        [FormerlySerializedAs("weapon")] public WeaponType Weapon;
        [FormerlySerializedAs("displayName")] public string DisplayName;
        [FormerlySerializedAs("color")] public Color Color = Color.white;

        [Header("見た目(仮モデル)")]
        [FormerlySerializedAs("headShape")] public PrimitiveType HeadShape = PrimitiveType.Cube;
        [FormerlySerializedAs("headScale")] public Vector3 HeadScale = new Vector3(0.08f, 0.08f, 1.2f);
        [FormerlySerializedAs("headOffset")] public Vector3 HeadOffset = new Vector3(0f, 0f, 0.7f);
        [FormerlySerializedAs("headRotation")] public Vector3 HeadRotation;
        [Tooltip("0より大きいと柄を描く")]
        [FormerlySerializedAs("handleLength")] public float HandleLength;

        [Header("ボーナス")]
        [Tooltip("3スロットがすべてこの武器種のとき使えるフィニッシャー")]
        [FormerlySerializedAs("masteryFinisher")] public SkillData MasteryFinisher;
        [Tooltip("シナジー/マスタリー時の、この武器種の技のひるみ値ボーナス")]
        [Range(0f, 1f)] [FormerlySerializedAs("synergyStaggerBonus")] public float SynergyStaggerBonus = 0.1f;
    }
}
