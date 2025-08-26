using System;
using System.Collections.Generic;
using System.Linq;
using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Windows;
using Code.Gameplay.Windows.Configs;
using Code.Infrastructure.WindowsService;
using UnityEngine;

namespace Code.Infrastructure.StaticData
{
  public class StaticDataService : IStaticDataService
  {
    private Dictionary<WindowId, GameObject> _windowPrefabsById;
    private Dictionary<ItemSpawnerTypeId, GameObject> _itemsSpawnerPrefabsById;

    public void LoadAll()
    {
      LoadWindows();
      // LoadItemsSpawner();
    }

    public GameObject GetWindowPrefab(WindowId id) =>
      _windowPrefabsById.TryGetValue(id, out GameObject prefab)
        ? prefab
        : throw new Exception($"Prefab config for window {id} was not found");
    
    // public GameObject GetItemSpawnerPrefab(ItemSpawnerTypeId id) =>
    //   _itemsSpawnerPrefabsById.TryGetValue(id, out GameObject prefab)
    //     ? prefab
    //     : throw new Exception($"Prefab config for window {id} was not found");

    private void LoadWindows()
    {
      _windowPrefabsById = Resources
        .Load<WindowsConfig>("Configs/Windows/windowsConfig")
        .WindowConfigs
        .ToDictionary(x => x.Id, x => x.Prefab);
    }
    
    // private void LoadItemsSpawner()
    // {
    //   // _itemsSpawnerPrefabsById = Resources
    //   //   .Load<ItemsSpawnerConfig>("Configs/ItemsSpawner/ItemSpawnerConfig")
    //   //   .ItemSpawnerConfigs
    //   //   .ToDictionary(x => x.Id, x => x.Prefab);
    // }
  }
}