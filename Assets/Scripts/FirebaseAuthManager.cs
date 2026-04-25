using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
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
    private FirebaseAuth auth;
    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
    }
    void Start()
    {
        UserName.onValidateInput += ValidateInput;
        Password.onValidateInput += ValidateInput;
        RePassWord.onValidateInput += ValidateInput;
        UserNameLogin.onValidateInput += ValidateInput;
        PasswordLogin.onValidateInput += ValidateInput;
    }
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
        string email = UserName.text + "@gmail.com";
        string password = Password.text;
        string repassword = RePassWord.text;
        if (password != repassword)
        {
            notificationRegister.gameObject.SetActive(true);
            notificationRegister.color = Color.red;
            notificationRegister.text = "Vui lòng nhập lại mật khẩu đúng với mật khẩu đã nhập ở trên";
            return;
        }
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
                            notificationRegister.text = "Username đã tồn tại";
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
                notificationRegister.text = "Đăng ký thành công! Mau quay về trang đăng nhập!";
                notificationRegister.color = Color.green;
                notificationRegister.gameObject.SetActive(true);
            }
        });
    }
    public void LoginFireBase()
    {
        string email = UserNameLogin.text + "@gmail.com";
        string password = PasswordLogin.text;

        if (string.IsNullOrEmpty(UserNameLogin.text) || string.IsNullOrEmpty(password))
        {
            notificationLogin.gameObject.SetActive(true);
            notificationLogin.color = Color.red;
            notificationLogin.text = "Vui lòng nhập đầy đủ thông tin";
            return;
        }

        LoginButton.interactable = false;

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
                            notificationLogin.text = "Username không tồn tại";
                            break;
                        case AuthError.WrongPassword:
                            notificationLogin.color = Color.red;
                            notificationLogin.text = "Sai mật khẩu";
                            break;
                        case AuthError.InvalidEmail:
                            notificationLogin.color = Color.red;
                            notificationLogin.text = "Username không hợp lệ";
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
    }
}
