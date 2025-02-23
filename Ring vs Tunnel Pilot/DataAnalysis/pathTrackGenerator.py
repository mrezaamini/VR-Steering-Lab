import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
import os


#TODO: in data analysis, KEEP in mind that preprocessing of points are done directly here using "adjust points"
#TODO: but adjusted points are not saved to anyfile. They should be saved and correctly processed. (ask what you should upload at the end as supp)


# Directory containing the data files
data_dir = '../Assets/CapturedData/'


# Load the CSV file
def load_data(file_path):
    return pd.read_csv(file_path)


# Function to process the file and plot each unique trial
def process_tracks(file_path):
    data = load_data(file_path)

    # Define the columns that identify a unique trial
    trial_columns = ['PID', 'taskType', 'rightHanded', 'width', 'length', 'rotation', 'trialRep']

    # Group data by unique trials
    grouped = data.groupby(trial_columns)

    for trial_id, trial_data in grouped:
        width = trial_data['width'].iloc[0]  # Get the width for the circle diameter
        plot_track(trial_data, trial_id, width, file_path)


# Function to adjust points outside the circle
def adjust_points(trial_data, radius):
    distances = np.sqrt(trial_data['PositionX'] ** 2 + trial_data['PositionY'] ** 2)
    outside_circle = distances > radius

    if outside_circle.any():
        scale_factor = radius / distances[outside_circle]
        trial_data.loc[outside_circle, 'PositionX'] *= scale_factor
        trial_data.loc[outside_circle, 'PositionY'] *= scale_factor

    return trial_data


# Function to plot a single track trial
def plot_track(trial_data, trial_id, width, file_path):
    radius = width / 2
    trial_data = adjust_points(trial_data, radius)

    plt.figure(figsize=(6, 6))

    # Scatter plot of adjusted positions
    plt.scatter(trial_data['PositionX'], trial_data['PositionY'], label='Track Points', alpha=0.7)

    # Draw the circle with given width as the diameter
    circle = plt.Circle((0, 0), radius, color='r', fill=False, linestyle='dashed')
    plt.gca().add_patch(circle)

    # Labels and title
    plt.xlabel('PositionX')
    plt.ylabel('PositionY')
    plt.title(f'File: {os.path.basename(file_path)}')
    plt.legend()

    # Show the plot
    plt.show()


# Process all CSV files in the directory except those containing 'summary'
def process_all_files(directory):
    for filename in os.listdir(directory):
        if filename.endswith('.csv') and 'summary' not in filename.lower():
            file_path = os.path.join(directory, filename)
            print(f'Processing file: {filename}')
            process_tracks(file_path)


# Run the processing on all valid files
process_all_files(data_dir)
