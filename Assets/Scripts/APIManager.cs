using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;


public class APIManager : MonoBehaviour
{
    string baseURL = "https://sid-restapi.onrender.com";

    [Header("Inputs")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    [Header("UI")]
    public TMP_Text scoreText;

    int currentScore = 0;

    

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        string username = PlayerPrefs.GetString("username");
        currentScore = PlayerPrefs.GetInt(username, 0);


        if (scene == "LoginScene")
        {
            if (IsLoggedIn())
            {
                SceneManager.LoadScene("MainScene");
            }
        }


        if (scene == "MainScene")
        {
            currentScore = PlayerPrefs.GetInt("score", 0);
            UpdateScoreUI();

            StartCoroutine(GetUsers()); 
        }
    }

    public void RegisterUser()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        StartCoroutine(RegisterRequest(username, password));
    }

    IEnumerator RegisterRequest(string username, string password)
    {
        string url = baseURL + "/api/usuarios/";

        string json = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Usuario registrado correctamente");
            Debug.Log(request.downloadHandler.text);

            
            SceneManager.LoadScene("LoginScene");
        }
        else
        {
            Debug.Log("Error en registro");
            Debug.Log(request.error);
            Debug.Log(request.downloadHandler.text);
        }
    }

    public void LoginUser()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        StartCoroutine(LoginRequest(username, password));
    }

    IEnumerator LoginRequest(string username, string password)
    {
        string url = baseURL + "/api/auth/login";

        string json = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login exitoso");
            Debug.Log(request.downloadHandler.text);


            LoginResponse res = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

            PlayerPrefs.SetString("token", res.token);

            PlayerPrefs.SetString("username", username);
            PlayerPrefs.Save();


            SceneManager.LoadScene("MainScene");
        }
        else
        {
            Debug.Log("Error en login");
            Debug.Log(request.error);
            Debug.Log(request.downloadHandler.text);
        }
    }

    public bool IsLoggedIn()
    {
        return PlayerPrefs.HasKey("token");
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("token");
        PlayerPrefs.Save();

        Debug.Log("Sesión cerrada");

        SceneManager.LoadScene("LoginScene");
    }

    public void AddScore()
    {
        currentScore += 10;

        string username = PlayerPrefs.GetString("username");
        PlayerPrefs.SetInt(username, currentScore);
        PlayerPrefs.Save();

        UpdateScoreUI();

        StartCoroutine(GetUsers());

        Debug.Log("Score actual: " + currentScore);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }

    public void GoToRegister()
    {
        SceneManager.LoadScene("RegisterScene");
    }

    public void GoToLogin()
    {
        SceneManager.LoadScene("LoginScene");
    }

    [Header("Leaderboard")]
    public Transform leaderboardContent;
    public GameObject leaderboardItemPrefab;
    [System.Serializable]
    public class PlayerData
    {
        public string username;
        public int score;
    }
    void LoadLeaderboard()
    {
        // Datos simulados
        PlayerData[] players = new PlayerData[]
        {
        new PlayerData { username = "Ana", score = 150 },
        new PlayerData { username = "Luis", score = 90 },
        new PlayerData { username = "Carlos", score = 200 },
        new PlayerData { username = "David", score = PlayerPrefs.GetInt("score", 0) }
        };

        // Ordenar de mayor a menor
        System.Array.Sort(players, (a, b) => b.score.CompareTo(a.score));

        // Limpiar lista anterior
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }

        // Crear UI
        for (int i = 0; i < players.Length; i++)
        {
            GameObject item = Instantiate(leaderboardItemPrefab, leaderboardContent);

            TMP_Text text = item.GetComponent<TMP_Text>();

            text.text = (i + 1) + ". " + players[i].username + " - " + players[i].score;
        }
    }
    IEnumerator GetUsers()
    {
        string url = baseURL + "/api/usuarios/";

        UnityWebRequest request = UnityWebRequest.Get(url);

      
        string token = PlayerPrefs.GetString("token");

        request.SetRequestHeader("x-token", token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            UsuariosResponse res = JsonUtility.FromJson<UsuariosResponse>(json);

            MostrarLeaderboard(res.usuarios);
        }
        else
        {
            Debug.Log("Error al obtener usuarios");
            Debug.Log(request.error);
            Debug.Log(request.downloadHandler.text);
        }
    }
    [System.Serializable]
    public class LoginResponse
    {
        public string token;
    }
    [System.Serializable]
    public class Usuario
    {
        public string username;
    }

    [System.Serializable]
    public class UsuariosResponse
    {
        public Usuario[] usuarios;
    }
    void MostrarLeaderboard(Usuario[] usuarios)
    {
        
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }

        
        PlayerData[] players = new PlayerData[usuarios.Length];

        for (int i = 0; i < usuarios.Length; i++)
        {
            int score = PlayerPrefs.GetInt(usuarios[i].username, 0);

            players[i] = new PlayerData
            {
                username = usuarios[i].username,
                score = score
            };
        }

        
        System.Array.Sort(players, (a, b) => b.score.CompareTo(a.score));

        
        for (int i = 0; i < players.Length; i++)
        {
            GameObject item = Instantiate(leaderboardItemPrefab, leaderboardContent);

            TMP_Text text = item.GetComponent<TMP_Text>();

            text.text = (i + 1) + ". " + players[i].username + " - " + players[i].score;


            string currentUser = PlayerPrefs.GetString("username");

            if (players[i].username == currentUser)
            {
                text.color = Color.yellow;
            }
        }
    }
}