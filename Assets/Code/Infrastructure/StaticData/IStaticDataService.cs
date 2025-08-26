using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Windows;
using Code.Infrastructure.WindowsService;
using UnityEngine;

namespace Code.Infrastructure.StaticData
{
  public interface IStaticDataService
  {
    void LoadAll();
    
    GameObject GetWindowPrefab(WindowId id);
    // GameObject GetItemSpawnerPrefab(ItemSpawnerTypeId id);
  }
}