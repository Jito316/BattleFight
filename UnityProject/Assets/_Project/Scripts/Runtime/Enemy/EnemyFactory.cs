using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// EnemyProfile から敵を出す。プレハブ(EnemyProfile.Prefab)があればそれを使い、なければ仮モデルを組み立てる。
    /// どちらでも、体力・攻撃などの中身は EnemyProfile の値で決める。
    /// </summary>
    public static class EnemyFactory
    {
        /// <summary>エリートの体に混ぜる色の割合(プレハブの見た目にも重ねる)</summary>
        const float mEliteTint = 0.35f;
        static readonly Color mCrownColor = new Color(1f, 0.82f, 0.25f);

        /// <param name="iSurfaceMaterial">色を付ける元のマテリアル。ビルドでシェーダーが削られないよう、シーンから参照させる</param>
        public static EnemyController Create(EnemyProfile iProfile, Vector3 iPosition, Quaternion iRotation, Transform iPlayer,
            Material iSurfaceMaterial)
        {
            var aMaterials = PrimitiveVisuals.RuntimeMaterials(iSurfaceMaterial);
            GameObject aRoot;
            if (iProfile.Prefab != null)
            {
                aRoot = Object.Instantiate(iProfile.Prefab, iPosition, iRotation);
                aRoot.name = iProfile.DisplayName;
                // エリートの色はプロファイルに入っているが、プレハブの見た目には入っていないので重ねる
                if (iProfile.Elite != EliteAffix.None) TintBody(CharacterVisual.Of(aRoot.transform), EliteMaker.TintColor(iProfile.Elite));
            }
            else
            {
                aRoot = BuildFallback(iProfile, aMaterials);
                aRoot.transform.SetPositionAndRotation(iPosition, iRotation);
            }
            aRoot.transform.localScale = Vector3.one * iProfile.Scale;

            // エリートは金の冠をかぶる
            var aVisual = CharacterVisual.Of(aRoot.transform);
            if (iProfile.Elite != EliteAffix.None && aVisual != null) PrimitiveVisuals.AddCrown(aVisual, aMaterials(PrimitiveVisuals.CrownName, mCrownColor));

            var aDamageable = aRoot.GetComponent<Damageable>();
            if (aDamageable == null) aDamageable = aRoot.AddComponent<Damageable>();
            aDamageable.Configure(Team.Enemy, iProfile.MaxHealth, iProfile.MaxArmor, iProfile.ArmorRegenDelay);

            var aEnemy = aRoot.GetComponent<EnemyController>();
            if (aEnemy == null) aEnemy = aRoot.AddComponent<EnemyController>();
            if (aRoot.GetComponent<CharacterAnimation>() == null) aRoot.AddComponent<CharacterAnimation>();
            aEnemy.Initialize(iProfile, iPlayer);
            return aEnemy;
        }

        /// <summary>プレハブがないとき: 当たりの大きさと部品を、Enemy_Base と同じ形でコードから組み立てる</summary>
        static GameObject BuildFallback(EnemyProfile iProfile, System.Func<string, Color, Material> iMaterials)
        {
            var aRoot = new GameObject(iProfile.DisplayName);
            var aController = aRoot.AddComponent<CharacterController>();
            ConfigureBody(aController);
            var aVisual = PrimitiveVisuals.BuildEnemy(iProfile, iMaterials);
            aVisual.transform.SetParent(aRoot.transform, false);
            return aRoot;
        }

        /// <summary>敵の当たりの大きさ(高さ 2m・半径 0.5m。プレハブの Enemy_Base も同じ)</summary>
        public static void ConfigureBody(CharacterController iController)
        {
            iController.center = new Vector3(0f, 1f, 0f);
            iController.height = 2f;
            iController.radius = 0.5f;
            iController.stepOffset = 0.3f;
        }

        /// <summary>体(武器以外)の色に iTint を混ぜる</summary>
        static void TintBody(CharacterVisual iVisual, Color iTint)
        {
            if (iVisual == null) return;
            foreach (var aRenderer in iVisual.GetComponentsInChildren<Renderer>())
            {
                if (aRenderer == iVisual.WeaponRenderer) continue;
                aRenderer.material.color = Color.Lerp(aRenderer.material.color, iTint, mEliteTint);
            }
        }
    }
}
