import pandas as pd
import numpy as np
import os

def generate_synthetic_data(num_samples=2000):
    np.random.seed(42)
    
    data = []
    
    for _ in range(num_samples):
        # Choose a persona
        persona = np.random.choice(['casual', 'whale', 'frustrated', 'bored', 'regular'], p=[0.3, 0.1, 0.2, 0.15, 0.25])
        
        # Default stats
        platform_id = np.random.choice([0, 1]) # 0=Android, 1=iOS
        
        if persona == 'casual':
            total_sessions = np.random.randint(1, 15)
            avg_duration = np.random.uniform(2, 10)
            days_since_login = np.random.randint(0, 10)
            total_spent = 0.0
            support_tickets = 0
            ads_watched = np.random.randint(1, 20)
            levels_completed = np.random.randint(1, 10)
            social_shares = np.random.randint(0, 3)
            win_rate = np.random.randint(60, 90)
            is_churned = 1 if (days_since_login > 7 and np.random.rand() < 0.6) else 0
            
        elif persona == 'whale':
            total_sessions = np.random.randint(30, 100)
            avg_duration = np.random.uniform(15, 45)
            days_since_login = np.random.randint(0, 3)
            total_spent = np.random.uniform(50, 500)
            support_tickets = np.random.choice([0, 1, 2], p=[0.7, 0.2, 0.1])
            ads_watched = np.random.randint(0, 5)
            levels_completed = np.random.randint(20, 90)
            social_shares = np.random.randint(2, 20)
            win_rate = np.random.randint(70, 95)
            is_churned = 0
            
        elif persona == 'frustrated':
            total_sessions = np.random.randint(2, 10)
            avg_duration = np.random.uniform(1, 8)
            days_since_login = np.random.randint(2, 20)
            total_spent = 0.0
            support_tickets = np.random.randint(0, 3)
            ads_watched = np.random.randint(0, 3)
            levels_completed = np.random.randint(0, 2)
            social_shares = 0
            win_rate = np.random.randint(5, 35) # Very low win rate
            is_churned = 1 if (days_since_login > 4 or np.random.rand() < 0.8) else 0
            
        elif persona == 'bored':
            total_sessions = np.random.randint(40, 100)
            avg_duration = np.random.uniform(1, 4) # Very short sessions
            days_since_login = np.random.randint(5, 25)
            total_spent = np.random.uniform(0, 10)
            support_tickets = 0
            ads_watched = np.random.randint(0, 2)
            levels_completed = np.random.randint(30, 95)
            social_shares = np.random.randint(0, 2)
            win_rate = np.random.randint(85, 100)
            is_churned = 1 if (days_since_login > 7 or np.random.rand() < 0.7) else 0
            
        else: # regular
            total_sessions = np.random.randint(10, 50)
            avg_duration = np.random.uniform(8, 18)
            days_since_login = np.random.randint(0, 7)
            total_spent = np.random.uniform(0, 30)
            support_tickets = np.random.choice([0, 1], p=[0.9, 0.1])
            ads_watched = np.random.randint(2, 15)
            levels_completed = np.random.randint(8, 45)
            social_shares = np.random.randint(0, 5)
            win_rate = np.random.randint(55, 80)
            is_churned = 1 if days_since_login > 5 else 0

        data.append({
            'TotalSessions': total_sessions,
            'AvgSessionDuration': avg_duration,
            'DaysSinceLastLogin': days_since_login,
            'TotalSpent': total_spent,
            'SupportTicketsOpen': support_tickets,
            'AdsWatched': ads_watched,
            'LevelsCompleted': levels_completed,
            'SocialShares': social_shares,
            'WinRatePercent': win_rate,
            'PlatformId': platform_id,
            'IsChurned': is_churned
        })
        
    df = pd.DataFrame(data)
    
    # Save CSV
    output_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(output_dir, 'player_profiles.csv')
    df.to_csv(output_path, index=False)
    print(f"Generated {num_samples} synthetic player profiles at {output_path}")

if __name__ == "__main__":
    generate_synthetic_data()
