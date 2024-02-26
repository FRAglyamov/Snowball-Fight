using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject escMenu;
    private bool _isMenuOpen = false;

    public TMP_InputField nicknameInputField;

    public static MainMenu Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _isMenuOpen = !_isMenuOpen;
            escMenu.SetActive(_isMenuOpen);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
