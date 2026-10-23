using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// キャラクター(プレイヤー・敵)の見た目の入口。プレハブの「Visual」に付け、コードが触る部分をここに登録する。
    /// Visual の下(モデル・マテリアル・Animator)は自由に差し替えてよい。ここに登録した参照だけは保つこと。
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        [SerializeField, Tooltip("体。のけぞり・つぶれ・ひねりはこれを動かし、残像はこれの下のメッシュを写す(空なら Visual そのもの)")]
        Transform mBody;
        [SerializeField, Tooltip("武器を持たせる手(プレイヤー)。武器のプレハブはこの下に置かれる")]
        Transform mHandAnchor;
        [SerializeField, Tooltip("頭の上。エリートの冠などを付ける(空なら Visual の上 2.25m)")]
        Transform mHeadAnchor;
        [SerializeField, Tooltip("敵が手に持つ武器。型を切り替えると、型の色で塗る(なくてもよい)")]
        Renderer mWeaponRenderer;
        [SerializeField, Tooltip("アニメーションを流す Animator(なくてもよい)。パラメーターは CharacterAnimation を参照")]
        Animator mAnimator;
        [SerializeField, Tooltip("コードで動かす仮の動き(のけぞり・つぶれ・ひねり・武器の振り)を使うか。Animator で動かすなら外す")]
        bool mProceduralMotion = true;

        public Transform Body => mBody != null ? mBody : transform;
        public Transform HandAnchor => mHandAnchor;
        public Transform HeadAnchor => mHeadAnchor;
        public Renderer WeaponRenderer => mWeaponRenderer;
        public Animator Animator => mAnimator;
        public bool ProceduralMotion
        {
            get => mProceduralMotion;
            set => mProceduralMotion = value;
        }

        /// <summary>コードで組み立てるとき(仮モデル・プレハブの生成)に参照を入れる</summary>
        public void Setup(Transform iBody, Transform iHandAnchor, Transform iHeadAnchor, Renderer iWeaponRenderer)
        {
            mBody = iBody;
            mHandAnchor = iHandAnchor;
            mHeadAnchor = iHeadAnchor;
            mWeaponRenderer = iWeaponRenderer;
        }

        /// <summary>キャラクターの見た目(なければ null)</summary>
        public static CharacterVisual Of(Component iCharacter) =>
            iCharacter != null ? iCharacter.GetComponentInChildren<CharacterVisual>(true) : null;
    }
}
