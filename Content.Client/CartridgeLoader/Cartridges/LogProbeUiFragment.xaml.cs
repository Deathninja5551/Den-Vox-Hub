// SPDX-FileCopyrightText: 2023 Chief-Engineer
// SPDX-FileCopyrightText: 2024 Milon
// SPDX-FileCopyrightText: 2025 BlitzTheSquishy
// SPDX-FileCopyrightText: 2025 Jakumba
// SPDX-FileCopyrightText: 2025 deltanedas
// SPDX-FileCopyrightText: 2025 sleepyyapril
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Linq; // DeltaV
using Content.Client._DV.CartridgeLoader.Cartridges; // DeltaV
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared._DV.CartridgeLoader.Cartridges; // DeltaV
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class LogProbeUiFragment : BoxContainer
{
    /// <summary>
    /// Action invoked when the print button gets pressed.
    /// </summary>
    public Action? OnPrintPressed;

    public LogProbeUiFragment()
    {
        RobustXamlLoader.Load(this);

        PrintButton.OnPressed += _ => OnPrintPressed?.Invoke();
    }

    // DeltaV begin - Update to handle both types of data
    public void UpdateState(LogProbeUiState state)
    {
        EntityName.Text = state.EntityName;
        PrintButton.Disabled = string.IsNullOrEmpty(state.EntityName);

        ProbedDeviceContainer.RemoveAllChildren();

        if (state.NanoChatData != null)
        {
            SetupNanoChatView(state.NanoChatData.Value);
            DisplayNanoChatData(state.NanoChatData.Value);
        }
        else
        {
            SetupAccessLogView();
            if (state.PulledLogs.Count > 0)
                DisplayAccessLogs(state.PulledLogs);
        }
    }

    private void SetupNanoChatView(NanoChatData data)
    {
        TitleLabel.Text = Loc.GetString("log-probe-header-nanochat");
        ContentLabel.Text = Loc.GetString("log-probe-label-message");

        // Show card info if available
        var cardInfo = new List<string>();
        if (data.CardNumber != null)
            cardInfo.Add(Loc.GetString("log-probe-card-number", ("number", $"#{data.CardNumber:D4}")));

        // Add recipient count
        cardInfo.Add(Loc.GetString("log-probe-recipients", ("count", data.Recipients.Count)));

        CardNumberLabel.Text = string.Join(" | ", cardInfo);
        CardNumberLabel.Visible = true;
    }

    private void SetupAccessLogView()
    {
        TitleLabel.Text = Loc.GetString("log-probe-header-access");
        ContentLabel.Text = Loc.GetString("log-probe-label-accessor");
        CardNumberLabel.Visible = false;
    }

    private void DisplayNanoChatData(NanoChatData data)
    {
        // First add a recipient list entry
        var recipientsList = Loc.GetString("log-probe-recipient-list") + "\n" + string.Join("\n",
            data.Recipients.Values
                .OrderBy(r => r.Name)
                .Select(r => $"    {r.Name}" +
                             (string.IsNullOrEmpty(r.JobTitle) ? "" : $" ({r.JobTitle})") +
                             $" | #{r.Number:D4}"));

        var recipientsEntry = new LogProbeUiEntry(0, "---", recipientsList);
        ProbedDeviceContainer.AddChild(recipientsEntry);

        var count = 1;
        foreach (var (partnerId, messages) in data.Messages)
        {
            // Show only successfully delivered incoming messages
            var incomingMessages = messages
                .Where(msg => msg.SenderId == partnerId && !msg.DeliveryFailed)
                .OrderByDescending(msg => msg.Timestamp);

            foreach (var msg in incomingMessages)
            {
                var messageText = Loc.GetString("log-probe-message-format",
                    ("sender", $"#{msg.SenderId:D4}"),
                    ("recipient", $"#{data.CardNumber:D4}"),
                    ("content", msg.Content));

                var entry = new NanoChatLogEntry(
                    count,
                    TimeSpan.FromSeconds(Math.Truncate(msg.Timestamp.TotalSeconds)).ToString(),
                    messageText);

                ProbedDeviceContainer.AddChild(entry);
                count++;
            }

            // Show only successfully delivered outgoing messages
            var outgoingMessages = messages
                .Where(msg => msg.SenderId == data.CardNumber && !msg.DeliveryFailed)
                .OrderByDescending(msg => msg.Timestamp);

            foreach (var msg in outgoingMessages)
            {
                var messageText = Loc.GetString("log-probe-message-format",
                    ("sender", $"#{msg.SenderId:D4}"),
                    ("recipient", $"#{partnerId:D4}"),
                    ("content", msg.Content));

                var entry = new NanoChatLogEntry(
                    count,
                    TimeSpan.FromSeconds(Math.Truncate(msg.Timestamp.TotalSeconds)).ToString(),
                    messageText);

                ProbedDeviceContainer.AddChild(entry);
                count++;
            }
        }
    }
    // DeltaV end

    // DeltaV - Handle this in a separate method
    private void DisplayAccessLogs(List<PulledAccessLog> logs)
    {
        //Reverse the list so the oldest entries appear at the bottom
        logs.Reverse();

        var count =  1;
        foreach (var log in logs)
        {
            AddAccessLog(log, count);
            count++;
        }
    }

    private void AddAccessLog(PulledAccessLog log, int numberLabelText)
    {
        var timeLabelText = TimeSpan.FromSeconds(Math.Truncate(log.Time.TotalSeconds)).ToString();
        var accessorLabelText = log.Accessor;
        var entry = new LogProbeUiEntry(numberLabelText, timeLabelText, accessorLabelText);

        ProbedDeviceContainer.AddChild(entry);
    }
}
