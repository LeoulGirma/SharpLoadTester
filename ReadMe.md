# SharpLoadTester

<!-- ![SharpLoadTester Logo](sharp-tester-logo.png) -->
SharpLoadTester is a powerful and flexible load testing tool developed in C#. It is specifically designed to generate a large volume of simultaneous HTTP requests, making it ideal for evaluating the performance, reliability, and scalability of web applications and APIs. By using SharpLoadTester, developers and businesses can identify potential bottlenecks and ensure their systems are robust enough to handle real-world traffic demands.

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
  - [Command-Line Options](#command-line-options)
  - [Examples](#examples)
- [Output & Reporting](#output--reporting)
- [Logging](#logging)
- [Contributing](#contributing)
- [License](#license)

## Features

- **High Concurrency:** Simulate thousands of concurrent HTTP requests.
- **Rate Limiting:** Control the number of requests per second.
- **Customizable Requests:** Support for various HTTP methods, custom headers, and request bodies.
- **Performance Metrics:** Collect detailed statistics including response times, CPU and memory usage, and data transfer.
- **Retry Mechanism:** Automatically retry failed requests with exponential backoff.
- **Threshold Monitoring:** Set response time thresholds to monitor performance compliance.
- **Real-Time Progress Reporting:** View live updates on test progress and system resource usage.
- **Detailed Reporting:** Generate comprehensive reports in both console and JSON formats.
- **Graceful Shutdown:** Handle interruptions gracefully, ensuring accurate reporting.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later

## Installation

### Option 1: Download Pre-built Executables (Recommended)

Download the latest release for your platform from the [Releases page](https://github.com/LeoulGirma/SharpLoadTester/releases/latest):

- **Windows (x64)**: `SharpLoadTester-v*-win-x64.zip`
- **Linux (x64)**: `SharpLoadTester-v*-linux-x64.zip`
- **macOS (Intel)**: `SharpLoadTester-v*-osx-x64.zip`
- **macOS (Apple Silicon)**: `SharpLoadTester-v*-osx-arm64.zip`

Extract the ZIP file and run the executable directly - no installation required!

### Option 2: Build from Source

1. **Clone the Repository**

   ```bash
   git clone https://github.com/LeoulGirma/SharpLoadTester.git
   cd SharpLoadTester
   ```

2. **Build the Project**

   ```bash
   dotnet build -c Release
   ```

3. **Publish (Optional)**

   To create a self-contained executable:

   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
   ```

   Replace `win-x64` with your target runtime identifier as needed.

## Usage

SharpLoadTester is a command-line tool. Below are the instructions on how to use it effectively.

### Command-Line Options

| Option                     | Description                                                                                          | Required | Default    |
| -------------------------- | ---------------------------------------------------------------------------------------------------- | -------- | ---------- |
| `--url`                    | **Target URL** to send requests to.                                                                 | Yes      | N/A        |
| `--requests-per-second`    | Number of requests to send per second.                                                               | No       | Unlimited  |
| `--total-requests`         | **Total number of requests** to send.                                                               | No       | `200`      |
| `--concurrency`            | Number of concurrent tasks to execute.                                                               | No       | `20`       |
| `--start-id`               | **Start ID** to append to the URL (useful for APIs requiring an identifier).                        | No       | N/A        |
| `--end-id`                 | **End ID** to append to the URL (useful for APIs requiring an identifier).                          | No       | N/A        |
| `--method`                 | **HTTP method** to use (e.g., GET, POST, PUT, DELETE).                                              | No       | `GET`      |
| `--headers`                | **Custom headers** in `'Key:Value'` format, separated by semicolons (`;`).                           | No       | N/A        |
| `--body`                   | **Request body** for POST/PUT requests.                                                             | No       | N/A        |
| `--output`                 | **Output file** to save results in JSON format.                                                      | No       | N/A        |
| `--threshold`              | **Response time threshold** in milliseconds to monitor performance compliance.                      | No       | N/A        |
| `--log-level`              | **Log level** for output verbosity (`debug`, `info`, `warning`, `error`).                           | No       | `info`     |
| `-h`, `--help`             | Show help information.                                                                               | No       | N/A        |

### Examples

1. **Basic Load Test**

   Send 1,000,000 GET requests to `https://example.com/api` with a concurrency of 100.

   ```bash
   SharpLoadTester --url https://example.com/api --total-requests 1000000 --concurrency 100
   ```

2. **POST Requests with Custom Headers and Body**

   Send 500,000 POST requests with custom headers and a JSON body.

   ```bash
   SharpLoadTester --url https://example.com/api/resource \
                  --method POST \
                  --headers "Content-Type:application/json;Authorization:Bearer token" \
                  --body '{"name":"test","value":123}' \
                  --total-requests 500000 \
                  --concurrency 150
   ```

3. **Rate-Limited Requests with Response Time Threshold**

   Send 100,000 GET requests at a rate of 500 requests per second, monitoring response times with a threshold of 200ms.

   ```bash
   SharpLoadTester --url https://example.com/api/resource \
                  --requests-per-second 500 \
                  --total-requests 100000 \
                  --concurrency 200 \
                  --threshold 200
   ```

4. **Appending IDs to URLs**

   Send requests with IDs appended to the base URL, ranging from 1 to 1000.

   ```bash
   SharpLoadTester --url https://example.com/api/resource \
                  --start-id 1 \
                  --end-id 1000 \
                  --total-requests 1000000 \
                  --concurrency 200
   ```

5. **Saving Detailed Report to a File**

   Save the test results in `report.json`.

   ```bash
   SharpLoadTester --url https://example.com/api \
                  --total-requests 500000 \
                  --concurrency 100 \
                  --output report.json
   ```

## Output & Reporting

Upon completion of a load test, SharpLoadTester provides a comprehensive report summarizing the performance metrics. The report includes:

- **General Metrics**
  - Elapsed Time
  - Total Successes and Failures
  - Success Rate (Status 200 OK)
  - Average Requests Per Second (RPS)

- **Throughput Statistics**
  - Minimum, Median, Average, and Maximum RPS
  - Standard Deviation of RPS
  - 1%, 5%, and 10% Low RPS

- **Response Time Statistics (ms)**
  - Average, Median, Minimum, and Maximum
  - Percentiles: P50, P75, P90, P95, P99

- **System Resource Usage**
  - CPU Usage (%): Minimum, Median, Average, Maximum
  - Memory Usage (MB): Minimum, Average, Maximum

- **Data Transfer**
  - Data Sent (MB)
  - Data Received (MB)

- **Threshold Statistics (if specified)**
  - Number and Percentage of Requests Below and Above the Threshold

- **Failure Logs**
  - Detailed information on failed requests, including URLs and error messages.

### JSON Report

If the `--output` option is specified, SharpLoadTester saves a detailed JSON report containing all the above metrics. This report can be used for further analysis or integration with other tools.

## Logging

SharpLoadTester provides real-time logging to keep you informed about the progress and performance of your load tests. The verbosity of the logs can be controlled using the `--log-level` option.

### Log Levels

- `debug`: Detailed diagnostic information.
- `info`: General operational messages about the progress.
- `warning`: Indications of potential issues.
- `error`: Error events that might still allow the application to continue running.

### Example

```bash
SharpLoadTester --url https://example.com/api \
               --total-requests 100000 \
               --concurrency 100 \
               --log-level debug
```

## Contributing

Contributions are welcome! Whether it's fixing bugs, improving performance, or adding new features, your help is greatly appreciated.

1. **Fork the Repository**

2. **Create a Feature Branch**

   ```bash
   git checkout -b feature/YourFeature
   ```

3. **Commit Your Changes**

   ```bash
   git commit -m "Add Your Feature"
   ```

4. **Push to the Branch**

   ```bash
   git push origin feature/YourFeature
   ```

5. **Open a Pull Request**

Please ensure your code follows the project's coding standards and includes appropriate tests.

## License

This project is licensed under the [MIT License](LICENSE). You are free to use, modify, and distribute this software as per the terms of the license.

---

*Happy Testing! 🚀*