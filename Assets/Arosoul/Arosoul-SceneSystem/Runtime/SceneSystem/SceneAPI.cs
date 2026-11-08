
namespace Arosoul.SceneSystem
{
    public static class SceneAPI
    {
        public static bool IsLoadingScene => SceneManager.Inst.IsBusy;

        public static int GetActiveBuildIndex => UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        public static string GetActiveName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;    

        /// <summary>
        /// Check if a scene is loaded inside of slot.
        /// </summary>
        public static bool IsSlotLoaded(string slotKey)
        {
            return SceneManager.IsSlotLoaded(slotKey);
        }

        /// <summary>
        /// Check if scene is loaded.
        /// </summary>
        /// <param name="sceneName">Name of scene to check.</param>
        /// <param name="slotKey">The slot which it is loaded in.</param>
        public static bool IsSceneLoaded(string sceneName, out string slotKey)
        {
            return SceneManager.IsSceneLoaded(sceneName, out slotKey);
        }

        public static SceneOperation NewOperation() 
        {
            return new SceneOperation();
        }
    }
}
