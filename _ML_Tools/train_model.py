
import pandas as pd
import numpy as np
import argparse
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType
import onnx
import os

def train_churn_model(csv_path, output_path):
    print(f"Loading data from {csv_path}...")
    
    try:
        df = pd.read_csv(csv_path)
    except Exception as e:
        print(f"Error loading CSV: {e}")
        return

    # 1. Preprocessing
    # Define features matching Unity's UserProfile.ToTensorArray order
    # [0] TotalSessions, [1] AvgSessionDuration, [2] DaysSinceLastLogin
    # [3] TotalSpent, [4] SupportTickets, [5] AdsWatched, [6] LevelsCompleted
    # [7] SocialShares, [8] WinRate, [9] PlatformId
    
    feature_cols = [
        'TotalSessions', 'AvgSessionDuration', 'DaysSinceLastLogin', 
        'TotalSpent', 'SupportTicketsOpen', 'AdsWatched', 
        'LevelsCompleted', 'SocialShares', 'WinRatePercent', 'PlatformId'
    ]
    
    # Check for missing columns and fill with defaults if necessary
    for col in feature_cols:
        if col not in df.columns:
            print(f"Warning: Column {col} missing, filling with 0.")
            df[col] = 0

    X = df[feature_cols].values.astype(np.float32)
    
    # Create target variable (y) based on 'DaysSinceLastLogin' or other logic if not explicit
    # Ideally, the simulation should output a 'Churned' label.
    # If not present, we infer: Churned if DaysSinceLastLogin > 14
    if 'IsChurned' in df.columns:
        y = df['IsChurned'].values
        print("Using provided 'IsChurned' label.")
    else:
        print("Inferring churn label (DaysSinceLastLogin > 14)...")
        y = (df['DaysSinceLastLogin'] > 14).astype(int)

    # Normalize features (matching Unity's NormalizeFeatures logic ideally, 
    # but Random Forest is robust to scale. However, neural networks usually need it.
    # Unity implementation does normalization on the *input* side before inference.
    # If we train on raw data, the model learns on raw ranges.
    # IMPORTANT: The Unity code normalizes INPUT before feeding to model. 
    # So we should probably train on NORMALIZED data or change Unity to not normalize?
    # Unity NormalizeFeatures: [0,1] clamping.
    # Decision: Let's TRAIN on raw data and REMOVE normalization in Unity for simplicity 
    # OR we replicate normalization here. 
    # Replicating is safer to ensure domain alignment.
    
    max_values = np.array([100, 60, 30, 1000, 10, 100, 100, 50, 100, 1])
    X_norm = X / max_values
    X_norm = np.clip(X_norm, 0, 1) # Clamp

    print(f"Training on {len(X)} samples...")
    X_train, X_test, y_train, y_test = train_test_split(X_norm, y, test_size=0.2, random_state=42)

    # 2. Train Model
    clf = RandomForestClassifier(n_estimators=100, max_depth=5, random_state=42)
    clf.fit(X_train, y_train)

    score = clf.score(X_test, y_test)
    print(f"Model Accuracy: {score:.2f}")

    # 3. Convert to ONNX
    print("Converting to ONNX...")
    initial_type = [('float_input', FloatTensorType([None, len(feature_cols)]))]
    onnx_model = convert_sklearn(clf, initial_types=initial_type)

    # 4. Save
    # Ensure directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "wb") as f:
        f.write(onnx_model.SerializeToString())
    
    print(f"Saved ONNX model to {output_path}")
    print("Done! You can now import this file into Unity Resources/Models/")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Train Retentia AI Churn Model")
    parser.add_argument("--input", default="player_profiles.csv", help="Input CSV file path")
    parser.add_argument("--output", default="churn_model.onnx", help="Output ONNX file path")
    
    args = parser.parse_args()
    
    if not os.path.exists(args.input):
        print(f"Error: Input file '{args.input}' not found.")
        print("Please run the Player Simulator in Unity Editor to generate profiles first.")
    else:
        train_churn_model(args.input, args.output)
