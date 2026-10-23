using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 武器の見た目の入口。武器のプレハブのいちばん上に付ける。プレハブの原点が手の位置、+Z が武器の先の向き。
    /// モデル・マテリアルは自由に差し替えてよい。先(Tip)だけは武器の先端に置くこと。
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        [SerializeField, Tooltip("武器の先。振ったときの軌跡はここから出る(空ならプレハブの原点)")]
        Transform mTip;
        [SerializeField, Tooltip("振ったときの軌跡。攻撃判定が出ている間だけ描く(空なら先に自動で付ける)")]
        TrailRenderer mTrail;

        public Transform Tip => mTip != null ? mTip : transform;
        public TrailRenderer Trail => mTrail;

        public void Setup(Transform iTip, TrailRenderer iTrail)
        {
            mTip = iTip;
            mTrail = iTrail;
        }
    }
}
