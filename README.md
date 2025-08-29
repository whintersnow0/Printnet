# Printnet

A high-performance C# web server that streams ASCII animations in real-time through HTTP endpoints. The server loads pre-computed animation data from JSON files and streams them as continuous text streams to connected clients.

## Features

- **Real-time ASCII Animation Streaming**: Stream animations as continuous HTTP responses
- **Smart Caching**: Automatically caches frequently accessed animations in memory
- **Adaptive Performance**: Automatically adjusts frame rates based on animation complexity
- **Memory Management**: Prevents large animations from overwhelming system memory
- **Size Optimization**: Automatically resizes oversized animations for optimal performance
- **Error Handling**: Graceful handling of missing files and client disconnections

## Requirements

- .NET 6.0 or later
- EmbedIO NuGet package

## Installation

1. Clone the repository or copy the source code
2. Install the required NuGet package:
```bash
dotnet add package EmbedIO
```
3. Build the project:
```bash
dotnet build
```

## Usage

### Running the Server

```bash
dotnet run
```

The server will start on port 5000 by default. You can specify a custom port using the `PORT` environment variable:

```bash
PORT=8080 dotnet run
```

### Animation File Structure

Create an `Anims` folder in the application directory and place your animation JSON files there. Each animation file should follow this format:

```json
{
  "framerate": 30,
  "frames": [
    [
      "  /\\_/\\  ",
      " ( o.o ) ",
      "  > ^ <  "
    ],
    [
      "  /\\_/\\  ",
      " ( ^.^ ) ",
      "  > - <  "
    ]
  ]
}
```

### Accessing Animations

Visit `http://localhost:5000/{animationName}` where `{animationName}` is the filename (without .json extension) of your animation.

Example:
- File: `Anims/cat.json`
- URL: `http://localhost:5000/cat`

## Animation Format Specifications

### JSON Structure

- `framerate`: Integer value specifying base frame rate (minimum 16 FPS)
- `frames`: Array of frames, where each frame is an array of strings representing lines of ASCII art

### Size Limitations

- Maximum frame size: 200x200 characters
- Animations larger than this will be automatically cropped
- Memory cache limit: 50MB per animation

### Performance Optimization

The server automatically adjusts frame delays based on content complexity:

- **Large frames (>50,000 chars)**: 3x base delay, minimum 100ms
- **Medium frames (>20,000 chars)**: 2x base delay, minimum 50ms  
- **Small frames (>5,000 chars)**: 1.5x base delay, minimum 30ms
- **Tiny frames**: Base delay, minimum 16ms

## API Endpoints

### `GET /{animationName}`

Streams the specified ASCII animation.

**Response Headers:**
- `Content-Type: text/plain; charset=utf-8`
- `Cache-Control: no-cache`
- `Connection: keep-alive`

**Status Codes:**
- `200`: Animation found and streaming
- `404`: Animation file not found
- `500`: Internal server error

## Performance Features

### Caching Strategy

- Animations under 50MB are automatically cached in memory
- Thread-safe cache access using lock mechanisms
- Cache persists until application restart

### Memory Management

- Precomputed frames are stored as byte arrays for efficient streaming
- Memory usage estimation prevents cache overflow
- Large animations are processed but not cached

### Adaptive Streaming

- Frame delays automatically adjust based on content size
- Handles client disconnections gracefully
- Optimized for continuous streaming without interruption

## Example Usage Scenarios

### Command Line Viewing
```bash
curl http://localhost:5000/myanimation
```

### Browser Viewing
Open `http://localhost:5000/myanimation` in any web browser to see the animation stream.

### Integration with Other Tools
The plain text stream format makes it easy to integrate with terminal applications, web frontends, or other tools that can consume HTTP streams.

## Configuration

### Environment Variables

- `PORT`: Server port (default: 5000)

### Performance Tuning

Modify these constants in the source code for different performance characteristics:

- `MAX_SQUARE_SIZE`: Maximum frame dimensions (default: 200)
- Cache memory limit: 50MB threshold for caching decisions

## Error Handling

The server handles various error conditions:

- **Missing animation files**: Returns 404 status
- **Malformed JSON**: Logs error and returns 500 status
- **Client disconnections**: Gracefully stops streaming
- **Large file processing**: Automatically resizes and continues

## Contributing

Feel free to submit issues and enhancement requests. When contributing code:

1. Maintain the existing code style
2. Add appropriate error handling
3. Consider performance implications for streaming
4. Test with various animation sizes and complexities

## License

This project is provided as-is for educational and practical use.
