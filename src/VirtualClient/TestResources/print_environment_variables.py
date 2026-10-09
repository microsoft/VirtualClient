import os

def print_environment_variables():
    """Prints all current environment variables formatted clearly."""
    print(f"{'ENVIRONMENT VARIABLE':<35} | VALUE")
    print("-" * 80)
    
    # Iterate through all environment variables sorted alphabetically
    for key, value in sorted(os.environ.items()):
        print(f"{key:<35} | {value}")

if __name__ == "__main__":
    print_environment_variables()