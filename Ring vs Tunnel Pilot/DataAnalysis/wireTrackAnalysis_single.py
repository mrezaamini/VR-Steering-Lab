import pandas as pd
import matplotlib.pyplot as plt
import numpy as np


def plot_coordinates(csv_file):
    # Read the CSV file
    try:
        data = pd.read_csv(csv_file)
        x = data['PositionX']
        y = data['PositionY']
        plt.figure(figsize=(8, 8), dpi=300)
        plt.scatter(x, y, color='blue', marker='o', label='Wire Track')

        ax = plt.gca()
        circle = plt.Circle((0, 0), 0.02, color='red', fill=False, linewidth=2, label='Ring')
        ax.add_artist(circle)
        ring_radius = 0.02
        ax.set_xlim(-ring_radius, ring_radius)
        ax.set_ylim(-ring_radius, ring_radius)

        #plt.title("Wire's trace inside the ring", fontsize=20)
        plt.xlabel("X-axis", fontsize=20)
        plt.ylabel("Y-axis",fontsize=20)
        plt.grid(True)
        plt.axhline(0, color='black', linewidth=0.8)
        plt.axvline(0, color='black', linewidth=0.8)

        #plt.legend(fontsize=20)

        # for managing ticks
        # x_ticks = np.arange(-ring_radius, ring_radius, 2)
        # y_ticks = np.arange(-ring_radius, ring_radius, 2)
        # plt.xticks(x_ticks, fontsize=20)
        # plt.yticks(y_ticks, fontsize=20)
        plt.xticks(fontsize=20, rotation=90)
        plt.yticks(fontsize=20)

        plt.tight_layout()
        plt.show()

    except FileNotFoundError:
        print(f"ERR: {csv_file} NOT FOUND!")
    except Exception as e:
        print(f"EXP: {e}")



csv_file_path = '../Assets/CapturedData/P1_T1_wireTrack.csv'
plot_coordinates(csv_file_path)
