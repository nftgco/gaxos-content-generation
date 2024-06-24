using System;
using System.Collections.Generic;
using ContentGeneration.Helpers;
using ContentGeneration.Models.Stability;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContentGeneration.Editor.MainWindow.Components.BasicExamples
{
    public class MaskGenerationTab : VisualElementComponent
    {
        public new class UxmlFactory : UxmlFactory<MaskGenerationTab, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
        
        public MaskGenerationTab()
        {
            var codeTextField = this.Q<TextField>("code");

            var prompt = this.Q<PromptInput>("promptInput");
            prompt.OnChanged += value =>
            {
                RefreshCode(codeTextField, value);
            };
            RefreshCode(codeTextField, prompt.value);

            var promptRequired = this.Q<Label>("promptRequiredLabel");
            promptRequired.style.visibility = Visibility.Hidden;

            var maskImage = this.Q<ImageSelection>("mask");
            var maskImageRequired = this.Q<Label>("maskImageRequiredLabel");
            maskImageRequired.style.visibility = Visibility.Hidden;

            var generateButton = this.Q<Button>("generateButton");
            var sendingRequest = this.Q<VisualElement>("sendingRequest");
            var requestSent = this.Q<VisualElement>("requestSent");
            generateButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (!generateButton.enabledSelf) return;

                requestSent.style.display = DisplayStyle.None;
                if (string.IsNullOrWhiteSpace(prompt.value))
                {
                    promptRequired.style.visibility = Visibility.Visible;
                    return;
                }

                if (maskImage.image == null)
                {
                    maskImageRequired.style.visibility = Visibility.Visible;
                    return;
                }

                generateButton.SetEnabled(false);
                sendingRequest.style.display = DisplayStyle.Flex;

                ContentGenerationApi.Instance.RequestStabilityMaskedImageGeneration
                (new StabilityMaskedImageParameters
                {
                    TextPrompts = new[]
                    {
                        new Prompt
                        {
                            Text = prompt.value,
                            Weight = 1,
                        }
                    },
                    InitImage = (Texture2D)maskImage.image
                }).ContinueInMainThreadWith(
                    t =>
                    {
                        try
                        {
                            if (t.IsFaulted)
                            {
                                Debug.LogException(t.Exception);
                            }
                            else
                            {
                                prompt.value = null;
                                requestSent.style.display = DisplayStyle.Flex;
                            }

                            generateButton.SetEnabled(true);
                            sendingRequest.style.display = DisplayStyle.None;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        MainWindowStore.Instance.RefreshRequestsAsync().CatchAndLog();
                    });
            });
        }

        void RefreshCode(TextField codeTextField, string promptText)
        {
            codeTextField.value =
                "var requestId = await ContentGenerationApi.Instance.RequestMaskedImageGeneration\n" +
                "\t(new StabilityMaskedImageParameters\n" +
                "\t{\n" +
                "\t\tTextPrompts = new[]\n" +
                "\t\t{\n" +
                "\t\t\tnew Prompt\n" +
                "\t\t\t{\n" +
                $"\t\t\t\tText = \"{promptText}\",\n" +
                "\t\t\t\tWeight = 1,\n" +
                "\t\t\t},\n" +
                "\t\t\tMaskImage = <Texture2D object>\n" +
                "\t\t}\n" +
                "\t})";
        }
    }
}