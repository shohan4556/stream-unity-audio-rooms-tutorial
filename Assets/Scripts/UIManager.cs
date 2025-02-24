using System;
using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.DeviceManagers;
using StreamVideo.Core.StatefulModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private ParticipantPanel _participantPanelPrefab;

    [SerializeField] private Transform _participantsContainer;

    [SerializeField] private TMP_InputField _callIdInput;

    [SerializeField] private Button _joinButton;

    [SerializeField] private Button _leaveButton;

    [SerializeField] private TMP_Dropdown _microphoneDropdown;

    [SerializeField] private AudioRoomsManager _audioRoomsManager;

    // Store list of available microphone device so we can retrieve them by index when user select option from the dropdown
    private readonly List<MicrophoneDeviceInfo> _microphoneDevices = new List<MicrophoneDeviceInfo>();

    // Stream's Audio Device Manager handles microphone interactions
    private IStreamAudioDeviceManager _audioDeviceManager;
    
    private readonly Dictionary<string, ParticipantPanel> _callParticipantBySessionId
        = new Dictionary<string, ParticipantPanel>();

    // Start is called automatically by Unity Engine
    private void Start()
    {
        var streamClient = _audioRoomsManager.StreamClient;
        _audioDeviceManager = streamClient.AudioDeviceManager;
        
        // add listeners to when participant joins or leaves the call
        _audioRoomsManager.ParticipantJoined += OnParticipantJoined;
        _audioRoomsManager.ParticipantLeft += OnParticipantLeft;
        
        // Add listeners to when user clicks on the buttons
        _joinButton.onClick.AddListener(OnJoinButtonClicked);
        _leaveButton.onClick.AddListener(OnLeaveButtonClicked);

        // Clear default options.
        _microphoneDropdown.ClearOptions();

        // Get available microphone devices
        var microphones = _audioDeviceManager.EnumerateDevices();

        // Store microphones in a list to later retrieve selected option by index
        _microphoneDevices.AddRange(microphones);

        // Get list of microphone names and populate the dropdown
        var microphoneLabels = _microphoneDevices.Select(d => d.Name).ToList();
        _microphoneDropdown.AddOptions(microphoneLabels);

        // Add listener method to when user changes microphone in the dropdown
        _microphoneDropdown.onValueChanged.AddListener(OnMicrophoneDeviceChanged);

        // Set first microphone device active. User can change active microphone via dropdown
        OnMicrophoneDeviceChanged(0);
    }

    private async void OnLeaveButtonClicked()
    {
        try
        {
            await _audioRoomsManager.LeaveCallAsync();
        
            foreach (var panel in _callParticipantBySessionId.Values)
            {
                Destroy(panel.gameObject);
            }
        
            _callParticipantBySessionId.Clear();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async void OnJoinButtonClicked()
    {
        if (string.IsNullOrEmpty(_callIdInput.text))
        {
            Debug.LogError("Please provide call ID");
            return;
        }

        try
        {
            await _audioRoomsManager.JoinCallAsync(_callIdInput.text);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnMicrophoneDeviceChanged(int deviceIndex)
    {
        var selectedMicrophone = _microphoneDevices[deviceIndex];

        // Select microphone and enable it meaning it will start capturing audio input immediately
        _audioDeviceManager.SelectDevice(selectedMicrophone, enable: true);
    }
    
    
    
    private void OnParticipantJoined(IStreamVideoCallParticipant participant)
    {
        var participantPanel = Instantiate(_participantPanelPrefab, _participantsContainer);
        participantPanel.Init(participant);

        // Save reference by Session ID so we can easily destroy when this participant leaves the call
        _callParticipantBySessionId.Add(participant.SessionId, participantPanel);
    }
    
    private void OnParticipantLeft(string sessionId)
    {
        if (!_callParticipantBySessionId.ContainsKey(sessionId))
        {
            // If participant is not found just ignore
            return;
        }

        var participantPanel = _callParticipantBySessionId[sessionId];
    
        // Destroy the game object representing a participant
        Destroy(participantPanel.gameObject);
    
        // Remove entry from the dictionary
        _callParticipantBySessionId.Remove(sessionId);
    }
    
    
}