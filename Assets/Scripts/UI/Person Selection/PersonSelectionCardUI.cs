using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PersonSelectionCardUI : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text ageLabel;
    [SerializeField] private Button startButton;
    [SerializeField] private Color missingPortraitColor = new Color(0.24f, 0.27f, 0.33f, 1f);
    [SerializeField] private Color portraitTint = Color.white;

    private PersonSelectionMenuController owner;
    private Person boundPerson;

    public Button StartButton => startButton;

    private void Awake()
    {
        AutoAssignMissingReferences();

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
            startButton.onClick.AddListener(HandleStartClicked);
        }
    }

    public void Bind(Person person, PersonSelectionMenuController menuController)
    {
        AutoAssignMissingReferences();

        owner = menuController;
        boundPerson = person;

        if (nameLabel != null)
            nameLabel.text = person != null ? person.DisplayName : "Unknown";

        if (ageLabel != null)
            ageLabel.text = person != null ? "Age " + person.DisplayAge : "Age --";

        if (portraitImage != null)
        {
            Sprite portrait = person != null ? person.Portrait : null;
            portraitImage.sprite = portrait;
            portraitImage.color = portrait != null ? portraitTint : missingPortraitColor;
        }

        if (startButton != null)
            startButton.interactable = person != null && person.flowchart != null;
    }

    private void HandleStartClicked()
    {
        if (owner == null || boundPerson == null)
            return;

        owner.SelectPerson(boundPerson);
    }

    private void AutoAssignMissingReferences()
    {
        if (portraitImage == null)
            portraitImage = FindRequiredComponent<Image>("Portrait");

        if (nameLabel == null)
            nameLabel = FindRequiredComponent<TMP_Text>("Name");

        if (ageLabel == null)
            ageLabel = FindRequiredComponent<TMP_Text>("Age");

        if (startButton == null)
            startButton = FindRequiredComponent<Button>("Start Button");
    }

    private T FindRequiredComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null)
            return null;

        return child.GetComponent<T>();
    }
}
