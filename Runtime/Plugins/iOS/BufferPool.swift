import Foundation

/// A thread-safe memory buffer pool for efficient buffer reuse in camera operations
final class BufferPool {
    /// Represents a reusable buffer in the pool
    private struct PooledBuffer {
        let pointer: UnsafeMutablePointer<UInt8>
        let capacity: Int
        var lastUsed: Date
        var isInUse: Bool
    }
    
    // Configuration
    private let maxBuffers: Int
    private let maxBufferAge: TimeInterval
    private let minBufferSize: Int
    private let maxBufferSize: Int
    
    // State management
    private let queue = DispatchQueue(label: "com.camera.bufferpool", attributes: .concurrent)
    private var pools: [Int: [PooledBuffer]] = [:] // Size -> Buffers mapping
    private var totalBuffers: Int = 0
    
    /// Initialize the buffer pool with configuration
    /// - Parameters:
    ///   - maxBuffers: Maximum number of buffers to keep in the pool
    ///   - maxBufferAge: Maximum age of unused buffers before recycling (in seconds)
    ///   - minBufferSize: Minimum buffer size to pool
    ///   - maxBufferSize: Maximum buffer size to pool
    init(maxBuffers: Int = 10, maxBufferAge: TimeInterval = 30, minBufferSize: Int = 1024, maxBufferSize: Int = 1024 * 1024 * 10) {
        self.maxBuffers = maxBuffers
        self.maxBufferAge = maxBufferAge
        self.minBufferSize = minBufferSize
        self.maxBufferSize = maxBufferSize
        
        // Start maintenance timer
        startMaintenanceTimer()
    }
    
    deinit {
        cleanup()
    }
    
    /// Acquire a buffer of the specified size
    /// - Parameter size: Required buffer size in bytes
    /// - Returns: A pointer to the allocated buffer
    func acquire(size: Int) -> UnsafeMutablePointer<UInt8> {
        // Don't pool if size is outside bounds
        guard size >= minBufferSize && size <= maxBufferSize else {
            return UnsafeMutablePointer<UInt8>.allocate(capacity: size)
        }
        
        // Round up to nearest power of 2 for better reuse
        let normalizedSize = nextPowerOfTwo(size)
        
        return queue.sync(flags: .barrier) {
            // Try to find an existing buffer
            if var buffers = pools[normalizedSize] {
                if let index = buffers.firstIndex(where: { !$0.isInUse && $0.capacity >= size }) {
                    buffers[index].isInUse = true
                    buffers[index].lastUsed = Date()
                    pools[normalizedSize] = buffers
                    return buffers[index].pointer
                }
            }
            
            // If we can't find a buffer or need to create a new one
            if totalBuffers < maxBuffers {
                let pointer = UnsafeMutablePointer<UInt8>.allocate(capacity: normalizedSize)
                let buffer = PooledBuffer(pointer: pointer, capacity: normalizedSize, lastUsed: Date(), isInUse: true)
                pools[normalizedSize, default: []].append(buffer)
                totalBuffers += 1
                return pointer
            }
            
            // If we've hit the buffer limit, force cleanup and try again
            performMaintenance(aggressive: true)
            
            // If still no room, just allocate new memory
            return UnsafeMutablePointer<UInt8>.allocate(capacity: size)
        }
    }
    
    /// Release a previously acquired buffer
    /// - Parameter pointer: Pointer to the buffer to release
    func release(_ pointer: UnsafeMutablePointer<UInt8>) {
        queue.async(flags: .barrier) {
            var found = false
            
            // Look for the pointer in our pools
            for (size, var buffers) in self.pools {
                if let index = buffers.firstIndex(where: { $0.pointer == pointer }) {
                    buffers[index].isInUse = false
                    buffers[index].lastUsed = Date()
                    self.pools[size] = buffers
                    found = true
                    break
                }
            }
            
            // If not found in pools, deallocate directly
            if !found {
                pointer.deallocate()
            }
            
            // Opportunistic maintenance
            if self.totalBuffers > self.maxBuffers / 2 {
                self.performMaintenance(aggressive: false)
            }
        }
    }
    
    /// Force cleanup of all buffers
    func cleanup() {
        queue.sync(flags: .barrier) {
            for (_, buffers) in pools {
                for buffer in buffers {
                    buffer.pointer.deallocate()
                }
            }
            pools.removeAll()
            totalBuffers = 0
        }
    }
    
    // MARK: - Private Methods
    
    private func startMaintenanceTimer() {
        Timer.scheduledTimer(withTimeInterval: maxBufferAge / 2, repeats: true) { [weak self] _ in
            self?.performMaintenance(aggressive: false)
        }
    }
    
    private func performMaintenance(aggressive: Bool) {
        queue.async(flags: .barrier) {
            let now = Date()
            var removedCount = 0
            
            // Clean up each size pool
            for (size, buffers) in self.pools {
                let updatedBuffers = buffers.filter { buffer in
                    let shouldKeep = buffer.isInUse || 
                        (!aggressive && now.timeIntervalSince(buffer.lastUsed) < self.maxBufferAge)
                    
                    if !shouldKeep {
                        buffer.pointer.deallocate()
                        removedCount += 1
                    }
                    return shouldKeep
                }
                
                if updatedBuffers.isEmpty {
                    self.pools.removeValue(forKey: size)
                } else {
                    self.pools[size] = updatedBuffers
                }
            }
            
            self.totalBuffers -= removedCount
        }
    }
    
    private func nextPowerOfTwo(_ n: Int) -> Int {
        var power: Int = 1
        while power < n {
            power *= 2
        }
        return power
    }
    
    /// Get current pool statistics
    var statistics: String {
        queue.sync {
            """
            Buffer Pool Statistics:
            Total Buffers: \(totalBuffers)
            Pools: \(pools.map { "Size \($0.key): \($0.value.count) buffers" }.joined(separator: "\n"))
            """
        }
    }
}