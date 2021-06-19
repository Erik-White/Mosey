using MahApps.Metro.Controls.Dialogs;
using Mosey.GUI.Models.Dialog;

namespace Mosey.GUI.Services.Dialog
{
    /// <summary>
    /// Extension methods to provide easy conversion between MahApps.Metro dialog
    /// </summary>
    internal static class DialogExtensions
    {
        /// <summary>
        /// Convert <see cref="BaseMetroDialog"/> to <see cref="IDialogInstance"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="IDialogInstance"/> instance</returns>
        internal static IDialogInstance ToDialogInstance(this BaseMetroDialog value)
        {
            return new DialogInstance(value.DialogSettings.ToDialogSettings())
            {
                Title = value.Title
            };
        }

        /// <summary>
        /// Convert <see cref="MetroDialogSettings"/> to <see cref="IDialogSettings"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="IDialogSettings"/> instance</returns>
        internal static IDialogSettings ToDialogSettings(this MetroDialogSettings value)
        {
            return new DialogSettings()
            {
                AffirmativeButtonText = value.AffirmativeButtonText,
                AnimateHide = value.AnimateHide,
                AnimateShow = value.AnimateShow,
                CancellationToken = value.CancellationToken,
                DefaultButtonFocus = value.DefaultButtonFocus.ToDialogResult(),
                DefaultText = value.DefaultText,
                DialogMessageFontSize = value.DialogMessageFontSize,
                DialogResultOnCancel = value.DialogResultOnCancel.ToDialogResult(),
                DialogTitleFontSize = value.DialogTitleFontSize,
                FirstAuxiliaryButtonText = value.FirstAuxiliaryButtonText,
                MaximumBodyHeight = value.MaximumBodyHeight,
                NegativeButtonText = value.NegativeButtonText,
                SecondAuxiliaryButtonText = value.SecondAuxiliaryButtonText
            };
        }

        /// <summary>
        /// Convert <see cref="IDialogSettings"/> to <see cref="MetroDialogSettings"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="MetroDialogSettings"/> instance</returns>
        internal static MetroDialogSettings ToMetroDialogSettings(this IDialogSettings value)
        {
            return new MetroDialogSettings()
            {
                AffirmativeButtonText = value.AffirmativeButtonText,
                AnimateHide = value.AnimateHide,
                AnimateShow = value.AnimateShow,
                CancellationToken = value.CancellationToken,
                DefaultButtonFocus = value.DefaultButtonFocus.ToMetroDialogResult(),
                DefaultText = value.DefaultText,
                DialogMessageFontSize = value.DialogMessageFontSize,
                DialogResultOnCancel = value.DialogResultOnCancel.ToMetroDialogResult(),
                DialogTitleFontSize = value.DialogTitleFontSize,
                FirstAuxiliaryButtonText = value.FirstAuxiliaryButtonText,
                MaximumBodyHeight = value.MaximumBodyHeight,
                NegativeButtonText = value.NegativeButtonText,
                SecondAuxiliaryButtonText = value.SecondAuxiliaryButtonText
            };
        }

        /// <summary>
        /// Covert <see cref="MessageDialogResult"/> to <see cref="DialogResult"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="DialogResult"/> instance</returns>
        internal static DialogResult ToDialogResult(this MessageDialogResult value)
        {
            return (DialogResult)value;
        }

        /// <summary>
        /// Covert <see cref="DialogResult"/> to <see cref="MessageDialogResult"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="MessageDialogResult"/> instance</returns>
        internal static MessageDialogResult ToMetroDialogResult(this DialogResult value)
        {
            return (MessageDialogResult)value;
        }

        /// <summary>
        /// Covert nullable <see cref="MessageDialogResult"/> to nullable <see cref="DialogResult"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="DialogResult"/> instance</returns>
        internal static DialogResult? ToDialogResult(this MessageDialogResult? value)
        {
            return (DialogResult?)value;
        }

        /// <summary>
        /// Convert nullable <see cref="DialogResult"/> to nullable <see cref="MessageDialogResult"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="MessageDialogResult"/> instance</returns>
        internal static MessageDialogResult? ToMetroDialogResult(this DialogResult? value)
        {
            return (MessageDialogResult?)value;
        }

        /// <summary>
        /// Convert <see cref="MessageDialogStyle"/> to <see cref="DialogStyle"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="DialogStyle"/> instance</returns>
        internal static DialogStyle ToMetroDialogStyle(this MessageDialogStyle value)
        {
            return (DialogStyle)value;
        }

        /// <summary>
        /// Convert <see cref="DialogStyle"/> to <see cref="MessageDialogStyle"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>An equivalent <see cref="MessageDialogStyle"/> instance</returns>
        internal static MessageDialogStyle ToMetroDialogStyle(this DialogStyle value)
        {
            return (MessageDialogStyle)value;
        }
    }
}
