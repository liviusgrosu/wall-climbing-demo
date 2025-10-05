using TMPro;
using UnityEngine;

public class DebugInterface : MonoBehaviour
{
    [SerializeField]
    private TMP_Text  relativeUpText;
    
    [SerializeField]
    private TMP_Text  isGroundedText;

    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text facingUpText;
    [SerializeField] private TMP_Text yawInvertedText;
    [SerializeField] private TMP_Text yawText;

    private void Start()
    {
        WallMovement.OnRelativeUpChanged += UpdateRelativeUpText;
        WallMovement.OnGroundedChanged += UpdateIsGroundedText;
        WallMovement.OnSpeedChange += UpdateSpeedText;
        WallMovement.OnFacingUpChange += UpdateFacingUpText;
        WallMovement.OnYawInvertedChange += UpdateYawInvertedText;
        WallMovement.OnYawChange += UpdateYawText;
    }

    private void UpdateRelativeUpText(Vector3 value)
    {
        relativeUpText.text = $"R Up: {value}";
    }
    
    private void UpdateIsGroundedText(bool value)
    {
        isGroundedText.text = $"Grounded: {GetColouredBoolText(value)}";
    }
    
    private void UpdateSpeedText(float value)
    {
        speedText.text = $"Speed: {value}";
    }
    
    private void UpdateFacingUpText(bool value)
    {
        facingUpText.text = $"Facing Up: {GetColouredBoolText(value)}";
    }
    
    private void UpdateYawInvertedText(bool value)
    {
        yawInvertedText.text = $"Yaw Inverted Up: {GetColouredBoolText(value)}";
    }
    
    private void UpdateYawText(float value)
    {
        yawText.text = $"Yaw: {value}";
    } 

    private static string GetColouredBoolText(bool value) => 
        value ? $"<color=#26D73A>T</color>" : $"<color=#FF0000>F</color>";
}
