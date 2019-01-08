﻿using LibVLCSharp.Shared.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LibVLCSharp.Shared
{
    /// <summary>
    /// Dialogs can be raised by libvlc for network actions and logins.
    /// You may only call once PostLogin or PostAction or Dismiss after which this instance will be invalid.
    /// </summary>
    public class Dialog
    {
        IntPtr _id;

        struct Native
        {
            [DllImport(Constants.LibraryName, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "libvlc_dialog_post_login")]
            internal static extern int LibVLCDialogPostLogin(IntPtr dialogId, IntPtr username, IntPtr password, bool store);

            [DllImport(Constants.LibraryName, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "libvlc_dialog_post_action")]
            internal static extern int LibVLCDialogPostAction(IntPtr dialogId, int actionIndex);

            [DllImport(Constants.LibraryName, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "libvlc_dialog_dismiss")]
            internal static extern int LibVLCDialogDismiss(IntPtr dialogId);
        }
        
        Dialog(IntPtr id)
        {
            if(id == IntPtr.Zero)
                throw new ArgumentNullException(nameof(id));
            _id = id;
        }

        internal Dialog(DialogId id) : this(id.NativeReference)
        {
        }

        /// <summary>
        /// Post a login answer.
        /// After this call, the instance won't be valid anymore
        /// </summary>
        /// <param name="username">valid non-empty string</param>
        /// <param name="password">valid string</param>
        /// <param name="store">if true stores the credentials</param>
        /// <returns></returns>
        public bool PostLogin(string username, string password, bool store)
        {
            if (_id == IntPtr.Zero)
                throw new VLCException("Calling method on dismissed Dialog instance");

            if (username == null)
                username = string.Empty;
            if (password == null)
                password = string.Empty;

            var usernamePtr = Utf8StringMarshaler.GetInstance().MarshalManagedToNative(username);
            var passwordPtr = Utf8StringMarshaler.GetInstance().MarshalManagedToNative(password);

            var result = Native.LibVLCDialogPostLogin(_id, usernamePtr, passwordPtr, store) == 0;

            _id = IntPtr.Zero;

            return result;
        }
        
        /// <summary>
        /// Post a question answer.
        /// After this call, this instance won't be valid anymore
        /// QuestionCb
        /// </summary>
        /// <param name="actionIndex">1 for action1, 2 for action2</param>
        /// <returns>return true on success, false otherwise</returns>
        public bool PostAction(int actionIndex)
        {
            if (_id == IntPtr.Zero)
                throw new VLCException("Calling method on dismissed Dialog instance");

            var result = Native.LibVLCDialogPostAction(_id, actionIndex) == 0;
            _id = IntPtr.Zero;

            return result;
        }

        /// <summary>
        /// Dismiss a dialog.
        /// After this call, this instance won't be valid anymore
        /// </summary>
        /// <returns>true if properly dismissed, false otherwise</returns>
        public bool Dismiss()
        {
            if (_id == IntPtr.Zero) return false;

            var result = Native.LibVLCDialogDismiss(_id) == 0;
            _id = IntPtr.Zero;

            return result;
        }
    }

    internal readonly struct DialogId
    {
        internal DialogId(IntPtr nativeReference)
        {
            NativeReference = nativeReference;
        }
        internal IntPtr NativeReference { get; }
    }

    public enum DialogQuestionType
    {
        Normal = 0,
        Warning = 1,
        Critical = 2
    }

    public delegate Task DisplayError(string title, string text);

    public delegate Task DisplayLogin(Dialog dialog, string title, string text, string defaultUsername, bool askStore, CancellationToken token);

    public delegate Task DisplayQuestion(Dialog dialog, string title, string text, DialogQuestionType type, string cancelText,
        string firstActionText, string secondActionText, CancellationToken token);

    public delegate Task DisplayProgress(Dialog dialog, string title, string text, bool indeterminate, float position, string cancelText, CancellationToken token);

    public delegate Task UpdateProgress(Dialog dialog, float position, string text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisplayErrorCallback(IntPtr data, string title, string text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisplayLoginCallback(IntPtr data, IntPtr dialogId, string title, string text,
        string defaultUsername, bool askStore);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisplayQuestionCallback(IntPtr data, IntPtr dialogId, string title, string text,
        DialogQuestionType type, string cancelText, string firstActionText, string secondActionText);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisplayProgressCallback(IntPtr data, IntPtr dialogId, string title, string text,
        bool indeterminate, float position, string cancelText);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void CancelCallback(IntPtr data, IntPtr dialogId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UpdateProgressCallback(IntPtr data, IntPtr dialogId, float position, string text);

    /// <summary>Dialog callbacks to be implemented</summary>
    internal readonly struct DialogCallbacks
    {
        internal DialogCallbacks(DisplayErrorCallback displayError, DisplayLoginCallback displayLogin, DisplayQuestionCallback displayQuestion,
            DisplayProgressCallback displayProgress, CancelCallback cancel, UpdateProgressCallback updateProgress)
        {
            DisplayError = displayError;
            DisplayLogin = displayLogin;
            DisplayQuestion = displayQuestion;
            DisplayProgress = displayProgress;
            Cancel = cancel;
            UpdateProgress = updateProgress;
        }

        internal readonly DisplayErrorCallback DisplayError;

        internal readonly DisplayLoginCallback DisplayLogin;

        internal readonly DisplayQuestionCallback DisplayQuestion;

        internal readonly DisplayProgressCallback DisplayProgress;

        internal readonly CancelCallback Cancel;

        internal readonly UpdateProgressCallback UpdateProgress;
    }
}