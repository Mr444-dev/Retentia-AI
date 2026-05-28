import torch
import torch.nn as nn

# Define a simple Neural Network (10 inputs -> 1 probability output)
class ChurnModel(nn.Module):
    def __init__(self):
        super(ChurnModel, self).__init__()
        self.fc1 = nn.Linear(10, 16)
        self.relu = nn.ReLU()
        self.fc2 = nn.Linear(16, 8)
        self.output = nn.Linear(8, 1)
        self.sigmoid = nn.Sigmoid()

    def forward(self, x):
        x = self.relu(self.fc1(x))
        x = self.relu(self.fc2(x))
        x = self.sigmoid(self.output(x))
        return x

# Export to ONNX
model = ChurnModel()
model.eval()
dummy_input = torch.randn(1, 10)
torch.onnx.export(model, dummy_input, "churn_model.onnx",
                  input_names=['input_features'],
                  output_names=['churn_probability'],
                  dynamic_axes={'input_features': {0: 'batch_size'}, 'churn_probability': {0: 'batch_size'}})

print("Model saved as churn_model.onnx")