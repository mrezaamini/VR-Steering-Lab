import pandas as pd
import matplotlib.pyplot as plt
import numpy as np


# Load the CSV file
def load_data(file_path):
    return pd.read_csv(file_path)


# Function to process the file and plot each unique trial
def process_tracks(file_path):
    data = load_data(file_path)

    # Define the columns that identify a unique trial
    trial_columns = ['PID', 'taskType', 'rightHanded', 'width', 'length', 'rotationX', 'rotationY', 'rotationZ',
                     'trialRep']

    # Group data by unique trials
    grouped = data.groupby(trial_columns)

    for trial_id, trial_data in grouped:
        width = trial_data['width'].iloc[0]  # Get the width for the circle diameter
        plot_track(trial_data, trial_id, width)



# Function to plot a single track trial
def plot_track(trial_data, trial_id, width):
    plt.figure(figsize=(6, 6))

    # Scatter plot of positions
    plt.scatter(trial_data['PositionX'], trial_data['PositionY'], label='Track Points', alpha=0.7)

    # Draw the circle with given width as the diameter
    circle = plt.Circle((0, 0), width / 2, color='r', fill=False, linestyle='dashed')
    plt.gca().add_patch(circle)

    # Labels and title
    plt.xlabel('PositionX')
    plt.ylabel('PositionY')
    plt.title(f'Track Trial: {trial_id}')
    plt.legend()

    # Show the plot
    plt.show()



# Example usage (replace 'your_file.csv' with the actual filename)
process_tracks('../Assets/CapturedData/P0_wireTracks.csv')