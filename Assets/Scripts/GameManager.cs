using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CJ.GodOfWar
{
    public class GameManager : MonoBehaviour
    {
        public UnityEvent OnStart, OnQuit;

        ThirdPersonUserControl input;
        bool m_Paused;

        private void Awake()
        {
            input = FindObjectOfType<ThirdPersonUserControl>();
        }

        private void Start()
        {
            OnStart.Invoke();
        }

        private void Update()
        {
            if (input.Esc)
                OnQuit.Invoke();
        }

        public void Restart()
        {
            if (m_Paused)
                PauseGame(false);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void PauseGame(bool pause)
        {
            m_Paused = pause;
            Time.timeScale = pause ? 0f:1.0f;
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                EditorApplication.isPlaying = false;
            #else
               Application.Quit();
            #endif
        }

        public void ToggleCursor(bool enable)
        {
            Cursor.visible = enable;
            Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}