// Check GPU access without starting Blender (which can crash when Metal returns nil).
#import <Foundation/Foundation.h>
#import <Metal/Metal.h>

int main(void) {
    @autoreleasepool {
        id<MTLDevice> device = MTLCreateSystemDefaultDevice();
        if (device == nil) {
            fprintf(stderr, "No Metal device is available to this process.\n");
            return 78;
        }
        printf("Metal device: %s\n", [[device name] UTF8String]);
        return 0;
    }
}
