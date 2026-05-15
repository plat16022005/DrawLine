using System.Collections;
using System.Collections.Generic;
#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseAuthManager : MonoBehaviour
{
    [Header("Đăng kí")]
    public TMP_InputField UserName;
    public TMP_InputField Password;
    public TMP_InputField RePassWord;
    public Button RegisterButton;
    public TextMeshProUGUI notificationRegister;
    [Header("Đăng nhập")]
    public TMP_InputField UserNameLogin;
    public TMP_InputField PasswordLogin;
    public Button LoginButton;
    public TextMeshProUGUI notificationLogin;
    [Header("Quên mật khẩu")]
    public TMP_InputField UserNameForgot;
    public Button ForgotButton;
    public TextMeshProUGUI notificationForgot;

    [Header("Notification Popup")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationPanelText;
    public Button closeNotificationButton;

#if !UNITY_WEBGL || UNITY_EDITOR
    private FirebaseAuth auth;
#endif
    private void Awake()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        auth = FirebaseAuth.DefaultInstance;
#endif
    }
    void Start()
    {
        // Password.onValidateInput += ValidateInput;
        // RePassWord.onValidateInput += ValidateInput;
        // PasswordLogin.onValidateInput += ValidateInput;

        if (closeNotificationButton != null)
        {
            closeNotificationButton.onClick.AddListener(() => notificationPanel.SetActive(false));
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Đăng ký events từ FirebaseJSBridge
        if (FirebaseJSBridge.instance != null)
        {
            FirebaseJSBridge.instance.OnSignInSucceeded    += HandleSignInSuccess;
            FirebaseJSBridge.instance.OnSignInFailure      += HandleSignInFailed;
            FirebaseJSBridge.instance.OnSignUpSucceeded    += HandleSignUpSuccess;
            FirebaseJSBridge.instance.OnSignUpFailure      += HandleSignUpFailed;
            FirebaseJSBridge.instance.OnPasswordResetSucceeded += HandlePasswordResetSuccess;
            FirebaseJSBridge.instance.OnPasswordResetFailure   += HandlePasswordResetFailed;
        }
        else
        {
            Debug.LogWarning("[FirebaseAuthManager] FirebaseJSBridge.instance is null! " +
                             "Please add a 'FirebaseJSBridge' GameObject to your initial scene.");
        }
#endif
    }
#if UNITY_WEBGL && !UNITY_EDITOR
    private void OnDestroy()
    {
        if (FirebaseJSBridge.instance != null)
        {
            FirebaseJSBridge.instance.OnSignInSucceeded    -= HandleSignInSuccess;
            FirebaseJSBridge.instance.OnSignInFailure      -= HandleSignInFailed;
            FirebaseJSBridge.instance.OnSignUpSucceeded    -= HandleSignUpSuccess;
            FirebaseJSBridge.instance.OnSignUpFailure      -= HandleSignUpFailed;
            FirebaseJSBridge.instance.OnPasswordResetSucceeded -= HandlePasswordResetSuccess;
            FirebaseJSBridge.instance.OnPasswordResetFailure   -= HandlePasswordResetFailed;
        }
    }
    // ── WebGL Auth Callbacks ──
    private void HandleSignInSuccess(string uid, string email)
    {
        LoginButton.interactable = true;
        notificationLogin.color = Color.green;
        notificationLogin.text = "Đăng nhập thành công!";
        notificationLogin.gameObject.SetActive(true);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Story");
    }
    private void HandleSignInFailed(string code, string message)
    {
        LoginButton.interactable = true;
        notificationLogin.color = Color.red;
        notificationLogin.gameObject.SetActive(true);
        switch (code)
        {
            case "auth/user-not-found":   notificationLogin.text = "Email không tồn tại"; break;
            case "auth/wrong-password":   notificationLogin.text = "Sai mật khẩu"; break;
            case "auth/invalid-email":    notificationLogin.text = "Email không hợp lệ"; break;
            case "auth/invalid-credential": notificationLogin.text = "Email hoặc mật khẩu không chính xác"; break;
            default:                      notificationLogin.text = "Lỗi: " + message; break;
        }
    }
    private void HandleSignUpSuccess(string uid, string email)
    {
        notificationRegister.text = "Đăng ký thành công!";
        notificationRegister.color = Color.green;
        notificationRegister.gameObject.SetActive(true);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Story");
    }
    private void HandleSignUpFailed(string code, string message)
    {
        notificationRegister.color = Color.red;
        notificationRegister.gameObject.SetActive(true);
        switch (code)
        {
            case "auth/email-already-in-use": notificationRegister.text = "Email đã tồn tại"; break;
            case "auth/weak-password":        notificationRegister.text = "Mật khẩu quá yếu"; break;
            case "auth/invalid-email":        notificationRegister.text = "Email không hợp lệ"; break;
            default:                          notificationRegister.text = "Lỗi đăng ký: " + message; break;
        }
    }
    private void HandlePasswordResetSuccess()
    {
        ForgotButton.interactable = true;
        notificationForgot.color = Color.green;
        notificationForgot.gameObject.SetActive(true);
        notificationForgot.text = "Gửi yêu cầu thành công!";
        OpenNotificationPanel("Đã gửi email khôi phục mật khẩu! Kiểm tra hộp thư của bạn.");
    }
    private void HandlePasswordResetFailed(string code, string message)
    {
        ForgotButton.interactable = true;
        notificationForgot.color = Color.red;
        notificationForgot.gameObject.SetActive(true);
        switch (code)
        {
            case "auth/user-not-found": notificationForgot.text = "Email không tồn tại trong hệ thống"; break;
            case "auth/invalid-email":  notificationForgot.text = "Email không hợp lệ"; break;
            default:                   notificationForgot.text = "Lỗi: " + message; break;
        }
    }
#endif
    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // Chỉ cho phép chữ + số
        if (char.IsLetterOrDigit(addedChar))
        {
            return addedChar;
        }

        // Nếu là ký tự đặc biệt → chặn
        return '\0';
    }
    public void RegisterFireBase()
    {
        string email = UserName.text;
        string password = Password.text;
        string repassword = RePassWord.text;
        if (password != repassword)
        {
            notificationRegister.gameObject.SetActive(true);
            notificationRegister.color = Color.red;
            notificationRegister.text = "Vui lòng nhập lại mật khẩu đúng với mật khẩu đã nhập ở trên";
            return;
        }
#if !UNITY_WEBGL || UNITY_EDITOR
        auth.CreateUserWithEmailAndPasswordAsync(email, password)
        .ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                var exception = task.Exception;
                FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;

                if (firebaseEx != null)
                {
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                    switch (errorCode)
                    {
                        case AuthError.EmailAlreadyInUse:
                            notificationRegister.color = Color.red;
                            notificationRegister.text = "Email đã tồn tại";
                            break;
                        case AuthError.WeakPassword:
                            notificationRegister.color = Color.red;
                            notificationRegister.text = "Mật khẩu quá yếu";
                            break;
                        default:
                            notificationRegister.color = Color.red;
                            notificationRegister.text = "Lỗi đăng ký";
                            break;
                    }
                }

                notificationRegister.gameObject.SetActive(true);
                return;
            }

            if (task.IsCompleted)
            {
                notificationRegister.text = "Đăng ký thành công!";
                notificationRegister.color = Color.green;
                notificationRegister.gameObject.SetActive(true);
                SceneManager.LoadScene("Story");
            }
        });
#else
        // WebGL: dùng FirebaseJSBridge
        if (FirebaseJSBridge.instance != null)
            FirebaseJSBridge.instance.SignUp(email, password);
        else
        {
            notificationRegister.color = Color.red;
            notificationRegister.gameObject.SetActive(true);
            notificationRegister.text = "Lỗi: FirebaseJSBridge không tỏn tại.";
        }
#endif
    }
    public void LoginFireBase()
    {
        string email = UserNameLogin.text;
        string password = PasswordLogin.text;

        if (string.IsNullOrEmpty(UserNameLogin.text) || string.IsNullOrEmpty(password))
        {
            notificationLogin.gameObject.SetActive(true);
            notificationLogin.color = Color.red;
            notificationLogin.text = "Vui lòng nhập đầy đủ thông tin";
            return;
        }

        LoginButton.interactable = false;

#if !UNITY_WEBGL || UNITY_EDITOR
        auth.SignInWithEmailAndPasswordAsync(email, password)
        .ContinueWithOnMainThread(task =>
        {
            LoginButton.interactable = true;

            if (task.IsFaulted)
            {
                var exception = task.Exception;
                FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;

                if (firebaseEx != null)
                {
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                    switch (errorCode)
                    {
                        case AuthError.UserNotFound:
                            notificationLogin.color = Color.red;
                            notificationLogin.text = "Email không tồn tại";
                            break;
                        case AuthError.WrongPassword:
                            notificationLogin.color = Color.red;
                            notificationLogin.text = "Sai mật khẩu";
                            break;
                        case AuthError.InvalidEmail:
                            notificationLogin.color = Color.red;
                            notificationLogin.text = "Email không hợp lệ";
                            break;
                        default:
                            notificationLogin.color = Color.red;
                            notificationLogin.text = "Tên đăng nhập hoặc mật khẩu không chính xác";
                            break;
                    }
                }

                notificationLogin.color = Color.red;
                notificationLogin.gameObject.SetActive(true);
                return;
            }

            if (task.IsCompleted)
            {
                FirebaseUser user = task.Result.User;

                notificationLogin.color = Color.green;
                notificationLogin.text = "Đăng nhập thành công!";
                notificationLogin.gameObject.SetActive(true);

                Debug.Log("User UID: " + user.UserId);

                SceneManager.LoadScene("Story");
            }
        });
#else
        // WebGL: dùng FirebaseJSBridge
        LoginButton.interactable = false;
        if (FirebaseJSBridge.instance != null)
            FirebaseJSBridge.instance.SignIn(email, password);
        else
        {
            LoginButton.interactable = true;
            notificationLogin.color = Color.red;
            notificationLogin.gameObject.SetActive(true);
            notificationLogin.text = "Lỗi: FirebaseJSBridge không tỏn tại.";
        }
#endif
    }

    public void SendForgotPasswordEmail()
    {
        if (string.IsNullOrEmpty(UserNameForgot.text))
        {
            notificationForgot.gameObject.SetActive(true);
            notificationForgot.color = Color.red;
            notificationForgot.text = "Vui lòng nhập Email đã đăng ký";
            return;
        }

        string email = UserNameForgot.text;
        ForgotButton.interactable = false;

#if !UNITY_WEBGL || UNITY_EDITOR
        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            ForgotButton.interactable = true;
            notificationForgot.gameObject.SetActive(true);

            if (task.IsFaulted)
            {
                var exception = task.Exception;
                FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;

                if (firebaseEx != null)
                {
                    AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                    switch (errorCode)
                    {
                        case AuthError.UserNotFound:
                            notificationForgot.color = Color.red;
                            notificationForgot.text = "Email không tồn tại trong hệ thống";
                            break;
                        case AuthError.InvalidEmail:
                            notificationForgot.color = Color.red;
                            notificationForgot.text = "Email không hợp lệ";
                            break;
                        default:
                            notificationForgot.color = Color.red;
                            notificationForgot.text = "Lỗi gửi email khôi phục";
                            break;
                    }
                }
                else
                {
                    notificationForgot.color = Color.red;
                    notificationForgot.text = "Lỗi kết nối";
                }
                return;
            }

            if (task.IsCompleted)
            {
                notificationForgot.color = Color.green;
                notificationForgot.text = "Gửi yêu cầu thành công!";
                OpenNotificationPanel("Yêu cầu khôi phục mật khẩu đã được gửi! Vui lòng kiểm tra hộp thư đến hoặc thư mục thư rác (Spam) trong email đã đăng ký của bạn.");
            }
        });
#else
        // WebGL: dùng FirebaseJSBridge
        ForgotButton.interactable = false;
        if (FirebaseJSBridge.instance != null)
            FirebaseJSBridge.instance.SendPasswordReset(email);
        else
        {
            ForgotButton.interactable = true;
            notificationForgot.gameObject.SetActive(true);
            notificationForgot.color = Color.red;
            notificationForgot.text = "Lỗi: FirebaseJSBridge không tồn tại.";
        }
#endif
    }

    private void OpenNotificationPanel(string message)
    {
        if (notificationPanel != null && notificationPanelText != null)
        {
            notificationPanelText.text = message;
            notificationPanel.SetActive(true);
        }
    }
}
