"""
Performance validation tests for spec input functionality.
"""

import unittest
import time
import sys
import os
from statistics import mean

# Add src to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../../src'))

from models.spec_input import SpecInput
from services.spec_input_service import SpecInputService


class TestSpecInputPerformance(unittest.TestCase):
    """Performance tests for spec input functionality."""
    
    def setUp(self):
        """Set up test fixtures."""
        self.service = SpecInputService()
        self.small_content = "This is a small test specification."
        self.medium_content = "This is a medium test specification. " * 50  # ~1.8KB
        self.large_content = "This is a large test specification. " * 1000   # ~36KB
        
    def _measure_operation_time(self, operation_func, iterations=10):
        """Measure average operation time."""
        times = []
        
        for _ in range(iterations):
            start_time = time.time()
            operation_func()
            end_time = time.time()
            times.append((end_time - start_time) * 1000)  # Convert to milliseconds
            
        return {
            'average_ms': mean(times),
            'max_ms': max(times)
        }
    
    def test_spec_creation_performance(self):
        """Test SpecInput creation performance."""
        def create_spec():
            spec = SpecInput(content=self.medium_content, format="text")
            return spec
            
        results = self._measure_operation_time(create_spec)
        print(f"\nSpec Creation: Avg {results['average_ms']:.2f}ms, Max {results['max_ms']:.2f}ms")
        
        self.assertLess(results['average_ms'], 10.0, "Spec creation should be under 10ms")
        
    def test_spec_processing_performance(self):
        """Test spec processing performance."""
        spec = SpecInput(content=self.medium_content, format="text")
        
        def process_spec():
            self.service.clear_processed_specs()
            return self.service.process_spec(spec)
            
        results = self._measure_operation_time(process_spec)
        print(f"\nSpec Processing: Avg {results['average_ms']:.2f}ms, Max {results['max_ms']:.2f}ms")
        
        self.assertLess(results['average_ms'], 50.0, "Spec processing should be under 50ms")
        self.assertLess(results['max_ms'], 200.0, "Max processing time should be under 200ms")
        
    def test_large_content_performance(self):
        """Test performance with large content."""
        large_spec = SpecInput(content=self.large_content, format="text")
        
        def process_large_spec():
            self.service.clear_processed_specs()
            return self.service.process_spec(large_spec)
            
        results = self._measure_operation_time(process_large_spec, iterations=5)
        print(f"\nLarge Content ({len(self.large_content)} chars): Avg {results['average_ms']:.2f}ms")
        
        self.assertLess(results['average_ms'], 200.0, "Large content processing should be under 200ms")


if __name__ == '__main__':
    unittest.main()