"""
Unit tests for spec input functionality.

These tests validate the actual implemented functionality.
"""

import unittest
import sys
import os

# Add src to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../../src'))

from models.spec_input import SpecInput
from services.spec_input_service import SpecInputService


class TestSpecInputModel(unittest.TestCase):
    """Test cases for SpecInput model."""
    
    def test_spec_input_creation_success(self):
        """Test that creating a valid spec input succeeds."""
        spec = SpecInput(content="This is a valid test specification")
        self.assertIsNotNone(spec)
        self.assertEqual(spec.content, "This is a valid test specification")
        self.assertEqual(spec.format, "text")
        self.assertTrue(spec.is_valid())
        
    def test_spec_input_with_markdown_format(self):
        """Test creating spec input with markdown format."""
        spec = SpecInput(content="# Test Spec\nThis is markdown", format="markdown")
        self.assertEqual(spec.format, "markdown")
        self.assertTrue(spec.is_valid())
        
    def test_spec_input_validation_empty_content(self):
        """Test that empty content is invalid."""
        spec = SpecInput(content="")
        self.assertFalse(spec.is_valid())
        
    def test_spec_input_validation_short_content(self):
        """Test that very short content is invalid."""
        spec = SpecInput(content="x")
        self.assertFalse(spec.is_valid())
        
    def test_spec_input_validation_invalid_format(self):
        """Test that invalid format is rejected."""
        spec = SpecInput(content="Valid content", format="invalid_format")
        self.assertFalse(spec.is_valid())
        
    def test_spec_input_sanitization(self):
        """Test content sanitization."""
        spec = SpecInput(content="<script>alert('test')</script>Valid content")
        sanitized = spec.sanitize()
        self.assertNotIn("<script>", sanitized)
        self.assertIn("Valid content", sanitized)
        
    def test_spec_input_to_dict(self):
        """Test conversion to dictionary."""
        spec = SpecInput(content="Test content", format="text")
        spec_dict = spec.to_dict()
        self.assertIn('content', spec_dict)
        self.assertIn('format', spec_dict)
        self.assertIn('is_valid', spec_dict)
        self.assertTrue(spec_dict['is_valid'])


class TestSpecInputService(unittest.TestCase):
    """Test cases for SpecInput service."""
    
    def setUp(self):
        """Set up test fixtures."""
        self.service = SpecInputService()
        
    def test_spec_processing_success(self):
        """Test successful spec processing."""
        spec = SpecInput(content="This is a comprehensive test specification for processing")
        result = self.service.process_spec(spec)
        self.assertTrue(result.success)
        self.assertIn("successfully", result.message.lower())
        
    def test_spec_processing_invalid_spec(self):
        """Test processing invalid spec raises ValueError."""
        spec = SpecInput(content="x")  # Too short
        with self.assertRaises(ValueError):
            self.service.process_spec(spec)
            
    def test_spec_processing_invalid_input_type(self):
        """Test processing with invalid input type."""
        result = self.service.process_spec("not a spec object")
        self.assertFalse(result.success)
        self.assertIn("Invalid input", result.message)
        
    def test_handle_invalid_spec_none(self):
        """Test handling None spec input."""
        result = self.service.handle_invalid_spec(None)
        self.assertFalse(result.success)
        self.assertIn("null specification", result.message)
        
    def test_processed_count_tracking(self):
        """Test that processed specs are tracked."""
        initial_count = self.service.get_processed_count()
        
        spec = SpecInput(content="Test specification for counting")
        self.service.process_spec(spec)
        
        self.assertEqual(self.service.get_processed_count(), initial_count + 1)


if __name__ == '__main__':
    unittest.main()