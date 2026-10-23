using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>
    /// プレハブの約束: アーティストが見た目を差し替えても、コードが触る部分(CharacterVisual・WeaponVisual)が残っているか。
    /// 敵・武器のデータがプレハブにつながっているか。
    /// </summary>
    public class PrefabContractTests
    {
        const string mPlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string mEnemyBasePrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Base.prefab";

        static T[] LoadAll<T>(string iFolder) where T : Object
        {
            var aGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { iFolder });
            var aAssets = new T[aGuids.Length];
            for (int aIndex = 0; aIndex < aGuids.Length; aIndex++)
            {
                aAssets[aIndex] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(aGuids[aIndex]));
            }
            return aAssets;
        }

        [Test]
        public void Player_Prefab_HasTheVisualContract()
        {
            var aPlayer = AssetDatabase.LoadAssetAtPath<GameObject>(mPlayerPrefabPath);
            Assert.NotNull(aPlayer, "プレイヤーのプレハブがない");
            var aVisual = aPlayer.GetComponentInChildren<CharacterVisual>(true);
            Assert.NotNull(aVisual, "プレイヤーに CharacterVisual がない");
            Assert.AreNotEqual(aVisual.transform, aVisual.Body, "体(Body)が登録されていない");
            Assert.NotNull(aVisual.HandAnchor, "手(HandAnchor)が登録されていない");
            Assert.NotNull(aPlayer.GetComponent<CharacterAnimation>(), "アニメーションの合図(CharacterAnimation)がない");
            Assert.NotNull(aPlayer.GetComponent<PlayerController>());
            Assert.NotNull(aPlayer.GetComponent<SkillExecutor>());
        }

        [Test]
        public void EveryEnemy_HasAPrefab_ThatIsAVariantOfTheBase()
        {
            var aBase = AssetDatabase.LoadAssetAtPath<GameObject>(mEnemyBasePrefabPath);
            Assert.NotNull(aBase, "敵の土台のプレハブがない");
            var aProfiles = LoadAll<EnemyProfile>("Assets/_Project/Data/Enemies");
            Assert.Greater(aProfiles.Length, 10);
            foreach (var aProfile in aProfiles)
            {
                Assert.NotNull(aProfile.Prefab, $"{aProfile.name} にプレハブがつながっていない");
                var aPrefab = aProfile.Prefab;
                Assert.NotNull(aPrefab.GetComponent<EnemyController>(), $"{aPrefab.name} に EnemyController がない");
                Assert.NotNull(aPrefab.GetComponent<Damageable>(), $"{aPrefab.name} に Damageable がない");
                Assert.NotNull(aPrefab.GetComponent<CharacterController>(), $"{aPrefab.name} に当たり(CharacterController)がない");
                Assert.NotNull(aPrefab.GetComponentInChildren<CharacterVisual>(true), $"{aPrefab.name} に CharacterVisual がない");
                Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(aPrefab), $"{aPrefab.name} が土台のバリアントではない");
                Assert.AreEqual(aBase, PrefabUtility.GetCorrespondingObjectFromSource(aPrefab), $"{aPrefab.name} の元が Enemy_Base ではない");
            }
        }

        [Test]
        public void SkillUsingEnemies_RegisterTheirWeapon()
        {
            foreach (var aProfile in LoadAll<EnemyProfile>("Assets/_Project/Data/Enemies"))
            {
                if (aProfile.SkillSets == null || aProfile.SkillSets.Count == 0) continue;
                var aVisual = aProfile.Prefab.GetComponentInChildren<CharacterVisual>(true);
                Assert.NotNull(aVisual.WeaponRenderer, $"{aProfile.name} は型の色で塗る武器を登録していない");
            }
        }

        [Test]
        public void EveryWeapon_HasAPrefab_WithATip()
        {
            var aWeapons = LoadAll<WeaponTypeData>("Assets/_Project/Data/Weapons");
            Assert.AreEqual(6, aWeapons.Length);
            foreach (var aWeapon in aWeapons)
            {
                Assert.NotNull(aWeapon.Prefab, $"{aWeapon.name} にプレハブがつながっていない");
                var aVisual = aWeapon.Prefab.GetComponent<WeaponVisual>();
                Assert.NotNull(aVisual, $"{aWeapon.Prefab.name} のいちばん上に WeaponVisual がない");
                Assert.AreNotEqual(aVisual.transform, aVisual.Tip, $"{aWeapon.Prefab.name} の先(Tip)が登録されていない");
            }
        }

        [Test]
        public void Prefabs_DoNotUseInstanceMaterials()
        {
            // プレハブにマテリアルのアセットでない物がつながると、保存されずに消える(見た目が真っ白・ピンクになる)
            foreach (var aPath in new[] { "Assets/_Project/Prefabs" })
            {
                foreach (var aGuid in AssetDatabase.FindAssets("t:Prefab", new[] { aPath }))
                {
                    var aPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(aGuid));
                    foreach (var aRenderer in aPrefab.GetComponentsInChildren<Renderer>(true))
                    {
                        foreach (var aMaterial in aRenderer.sharedMaterials)
                        {
                            Assert.NotNull(aMaterial, $"{aPrefab.name}/{aRenderer.name} のマテリアルが外れている");
                            Assert.IsTrue(AssetDatabase.Contains(aMaterial), $"{aPrefab.name}/{aRenderer.name} のマテリアルがアセットではない");
                        }
                    }
                }
            }
        }
    }
}
