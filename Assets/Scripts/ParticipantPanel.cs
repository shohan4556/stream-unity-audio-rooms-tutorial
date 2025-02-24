using System;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using UnityEngine;
using UnityEngine.Android;

public class ParticipantPanel : MonoBehaviour
{
    private void Awake()
    {
        // Request microphone permissions
        Permission.RequestUserPermission(Permission.Microphone);

        // Check if user granted microphone permission
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            // Notify user that microphone permission was not granted and the microphone capturing will not work.
        }
    }

    public void Init(IStreamVideoCallParticipant participant)
    {
        _participant = participant;

        // Add debug name so we can see in the Unity Editor which participant this game object represents.
        gameObject.name = $"Participant - {participant.Name} ({participant.SessionId})";

        // Process already available tracks
        foreach (var track in _participant.GetTracks())
        {
            OnTrackAdded(_participant, track);
        }

        // Subscribe to TrackAdded - this way we'll handle any track added in the future
        _participant.TrackAdded += OnTrackAdded;
    }

    private void OnTrackAdded(IStreamVideoCallParticipant participant, IStreamTrack track)
    {
        Debug.Log($"Track of type `{track.GetType()}` added for {_participant.Name}");

        // For this tutorial we only care for audio tracks but video tracks are also possible
        if (track is StreamAudioTrack streamAudioTrack)
        {
            // Create AudioSource
            _audioOutputAudioSource = GetComponent<AudioSource>();

            // Set this AudioSource to receive participant's audio stream
            streamAudioTrack.SetAudioSourceTarget(_audioOutputAudioSource);
        }
    }

    // Unity's special method called when object is destroyed
    private void OnDestroy()
    {
        // It's a good practice to always unsubscribe from events
        _participant.TrackAdded -= OnTrackAdded;
    }

    // This AudioSource will play the audio received from the participant
    private AudioSource _audioOutputAudioSource;

    // Keep reference so we can unsubscribe from events in OnDestroy
    private IStreamVideoCallParticipant _participant;
}