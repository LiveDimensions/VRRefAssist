using System;

namespace VRRefAssist.Editor.Exceptions
{
    public class OnBuildException : Exception
    {
        public bool logException { get; }

        internal bool showDialog;
        private OnBuildDialog _dialog;

        public OnBuildDialog dialog
        {
            set
            {
                _dialog = value;
                showDialog = _dialog != null;
            }
            get => _dialog;
        }

        public OnBuildException(string message, bool logException = false) : base(message)
        {
            this.logException = logException;
        }
    }

    public class OnBuildDialog
    {
        private readonly string title;
        private readonly string message;
        private readonly DialogType type;
        private DialogButton okButton;
        private DialogButton cancelButton;
        private DialogButton altButton;

        public OnBuildDialog(string title, string message, DialogButton okButton)
        {
            type = DialogType.Acknowledge;

            this.title = title;
            this.message = message;
            this.okButton = okButton;
        }

        public OnBuildDialog(string title, string message, DialogButton okButton, DialogButton cancelButton)
        {
            type = DialogType.Regular;

            this.title = title;
            this.message = message;
            this.okButton = okButton;
            this.cancelButton = cancelButton;
        }

        public OnBuildDialog(string title, string message, DialogButton okButton, DialogButton cancelButton, DialogButton altButton)
        {
            type = DialogType.Complex;

            this.title = title;
            this.message = message;
            this.okButton = okButton;
            this.cancelButton = cancelButton;
            this.altButton = altButton;
        }

        private int option;

        public Result ShowDialog()
        {
            switch (type)
            {
                default:
                case DialogType.Acknowledge:
                    UnityEditor.EditorUtility.DisplayDialog(title, message, okButton.text);
                    option = 0;
                    break;
                case DialogType.Regular:
                    option = UnityEditor.EditorUtility.DisplayDialog(title, message, okButton.text, cancelButton.text) ? 0 : 1;
                    break;
                case DialogType.Complex:
                    option = UnityEditor.EditorUtility.DisplayDialogComplex(title, message, okButton.text, cancelButton.text, altButton.text);
                    break;
            }

            switch (option)
            {
                default:
                case 0:
                    return okButton.GetResult();
                case 1:
                    return cancelButton.GetResult();
                case 2:
                    return altButton.GetResult();
            }
        }

        private enum DialogType
        {
            Acknowledge,
            Regular,
            Complex
        }
    }

    public readonly struct DialogButton
    {
        public readonly string text;
        private readonly bool cancelBuild;
        private readonly Func<Result> action;

        public DialogButton(string text, bool cancelBuild = true, Func<Result> action = null)
        {
            this.text = text;
            this.cancelBuild = cancelBuild;
            this.action = action;
        }

        public Result GetResult()
        {
            if (action == null)
            {
                return cancelBuild ? Result.CancelBuild : Result.ContinueBuild;
            }

            return action.Invoke();
        }
    }
    
    public enum Result
    {
        ContinueBuild,
        CancelBuild
    }
}