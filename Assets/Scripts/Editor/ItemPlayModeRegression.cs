#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Astraparasitus.Editor
{
    [InitializeOnLoad]
    public static class ItemPlayModeRegression
    {
        private const string PendingKey = "3DAdventure.ItemPlayModeRegression.Pending";
        private static int waitFrames;

        static ItemPlayModeRegression()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/3D Adventure/Run Item PlayMode Regression")]
        public static void Run()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Level01")
            {
                Debug.LogError("Open Level01 before running the item PlayMode regression.");
                return;
            }

            waitFrames = 0;
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying)
                return;

            GamePlay game = GamePlay.Instance;
            BackPack bag = BackPack.Instance;
            BackPackUI ui = UnityEngine.Object.FindFirstObjectByType<BackPackUI>(FindObjectsInactive.Include);
            GameContentCatalog catalog = GameContentCatalog.Default;
            if (game == null || game.player == null || bag == null || ui == null || catalog == null)
            {
                if (++waitFrames < 300) return;
                Finish($"Level01 dependencies missing: GamePlay={game != null}, Player={game != null && game.player != null}, BackPack={bag != null}, BackPackUI={ui != null}, Catalog={catalog != null}.");
                return;
            }

            try
            {
                Check(game, bag, ui, catalog);
                Finish(null);
            }
            catch (Exception exception)
            {
                Finish(exception.ToString());
            }
        }

        private static void Check(GamePlay game, BackPack bag, BackPackUI ui, GameContentCatalog catalog)
        {
            if (game.backpackUI != null && !game.backpackUI.activeSelf) game.backpackUI.SetActive(true);
            if (!ui.gameObject.activeInHierarchy) ui.gameObject.SetActive(true);
            Require(BackPackUI.Instance != null, "BackPackUI did not initialize when opened.");
            Require(catalog.TryGetItem(0, out ItemConfig ammo), "ID 0 must resolve to pistol ammo.");
            Require(catalog.TryGetItem(30, out ItemConfig box) && box.itemName == "Box", "Box must use ID 30.");
            Require(catalog.TryGetItem("acfb059d66ea4826bb604e11669b08b7", out ItemConfig oldAmmo) && oldAmmo == ammo,
                "The removed ammo asset's stable ID must resolve to the canonical ammo.");
            Require(catalog.TryGetItem(20, out ItemConfig spray), "Spray is missing.");
            Require(catalog.TryGetItem(21, out ItemConfig green), "Green powder is missing.");
            Require(catalog.TryGetItem(22, out ItemConfig blue), "Blue powder is missing.");
            Require(catalog.TryGetItem(23, out ItemConfig mixed), "Mixed powder is missing.");

            bag.RestoreState(new InventoryState());
            PickUp(ammo.worldPrefab, 10, ammo, bag, game);
            GameObject singleAmmo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Item/ItemTest 1.prefab");
            PickUp(singleAmmo, 11, ammo, bag, game);
            Require(bag.GetItemAmount(0) == 11, "Ten and one ammo pickups did not stack.");

            bag.RestoreState(new InventoryState());
            PickUp(green.worldPrefab, 1, green, bag, game);
            ui.Refresh();
            ItemUI greenIcon = FindIcon(ui, green);
            Require(greenIcon != null && greenIcon.icon.sprite == green.icon, "Green inventory icon is incorrect.");

            game.player.SetCurrentHp(20f);
            greenIcon.UseItem();
            Require(Mathf.Approximately(game.player.GetHp(), 70f), "Green powder did not heal by 50.");
            Require(bag.GetItemAmount(green.id) == 0, "Healing did not consume one green powder.");

            bag.AddItem(green);
            bag.AddItem(blue);
            ui.Refresh();
            greenIcon = FindIcon(ui, green);
            Require(greenIcon != null, "Green powder disappeared from the inventory UI.");
            greenIcon.CombineItem();
            Require(bag.GetItemAmount(green.id) == 0 && bag.GetItemAmount(blue.id) == 0 && bag.GetItemAmount(mixed.id) == 1,
                "Blue-green combination did not consume ingredients and create the result.");

            ui.Refresh();
            ItemUI mixedIcon = FindIcon(ui, mixed);
            Require(mixedIcon != null && mixedIcon.icon.sprite == mixed.icon, "Mixed powder inventory icon is incorrect.");
            mixedIcon.DropItem();
            Require(bag.GetItemAmount(mixed.id) == 0, "Dropping did not remove one mixed powder.");
            ItemBase dropped = FindDropped(mixed);
            Require(dropped != null && dropped.pickupAmount == 1, "Drop did not create a one-item pickup.");
            dropped.Interact();
            CloseInspect(game);
            Require(bag.GetItemAmount(mixed.id) == 1, "Dropped item could not be picked up again.");

            var legacy = new InventoryState();
            legacy.items.Add(new InventoryItemData { id = 0, amount = 2, item = new Item { id = 0, itemName = "Box", itemNum = 2 } });
            legacy.items.Add(new InventoryItemData { id = 0, itemId = "acfb059d66ea4826bb604e11669b08b7", amount = 3,
                item = new Item { id = 0, itemName = "Pistol Ammo", itemNum = 3 } });
            bag.RestoreState(legacy);
            Require(bag.GetItemAmount(box.id) == 2 && bag.GetItemAmount(ammo.id) == 3,
                "Old ID 0 inventory entries were not migrated to the right items.");
        }

        private static void PickUp(GameObject prefab, int expectedCount, ItemConfig config, BackPack bag, GamePlay game)
        {
            Require(prefab != null, $"{config.itemName} world prefab is missing.");
            GameObject instance = UnityEngine.Object.Instantiate(prefab, new Vector3(1000f, 1000f, 1000f), prefab.transform.rotation);
            ItemBase pickup = instance.GetComponentInChildren<ItemBase>();
            Require(pickup != null && pickup.itemConfig == config, $"{config.itemName} pickup references the wrong asset.");
            pickup.Interact();
            CloseInspect(game);
            Require(bag.GetItemAmount(config.id) == expectedCount, $"{config.itemName} pickup amount is incorrect.");
        }

        private static ItemUI FindIcon(BackPackUI ui, ItemConfig config)
        {
            foreach (ItemUI icon in ui.itemSlot.GetComponentsInChildren<ItemUI>(true))
                if (icon.nameText != null && icon.nameText.text == config.itemName) return icon;
            return null;
        }

        private static ItemBase FindDropped(ItemConfig config)
        {
            foreach (ItemBase pickup in UnityEngine.Object.FindObjectsByType<ItemBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (pickup.itemConfig == config && pickup.gameObject.name.EndsWith("(Clone)", StringComparison.Ordinal)) return pickup;
            return null;
        }

        private static void CloseInspect(GamePlay game)
        {
            if (game.ItemUI != null) game.ItemUI.SetActive(false);
            if (game.ItemName != null) game.ItemName.gameObject.SetActive(false);
            GamePause.Release(PauseReason.ItemInspect);
            if (!GamePause.IsPaused) game.player.playerInput.Enable();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(string error)
        {
            SessionState.SetBool(PendingKey, false);
            if (error == null) Debug.Log("Item PlayMode Regression passed: ammo, pickup, icons, healing, combination, drop/re-pickup, legacy save IDs.");
            else Debug.LogError($"Item PlayMode Regression failed: {error}");
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
