"""
Main application integration for spec input functionality.

This module provides the integration point between the spec input functionality
and the main NorthstarET LMS application.
"""

import sys
import os
import logging
from typing import Dict, Any, Optional

# Add src to path for imports
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'src'))

from models.spec_input import SpecInput
from services.spec_input_service import SpecInputService, SpecInputProcessingResult


class SpecInputApp:
    """
    Main application class for spec input functionality.
    
    This class provides the main interface for the spec input feature
    within the NorthstarET LMS system.
    """
    
    def __init__(self, log_level: str = "INFO"):
        """
        Initialize the spec input application.
        
        Args:
            log_level (str): Logging level (DEBUG, INFO, WARNING, ERROR)
        """
        # Configure logging
        logging.basicConfig(
            level=getattr(logging, log_level.upper()),
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )
        
        self.logger = logging.getLogger(__name__)
        self.logger.info("Initializing SpecInput application")
        
        # Initialize services
        self.service = SpecInputService(logger=self.logger)
        
    def process_specification(self, content: str, format: str = "text") -> Dict[str, Any]:
        """
        Main method to process a specification through the application.
        
        Args:
            content (str): The specification content
            format (str): The format of the content
            
        Returns:
            Dict[str, Any]: Processing result with status and data
        """
        try:
            # Create spec input
            spec_input = SpecInput(content=content, format=format)
            
            # Validate before processing
            if not spec_input.is_valid():
                return {
                    'status': 'error',
                    'message': 'Invalid specification input',
                    'errors': self._get_validation_errors(spec_input)
                }
            
            # Process the specification
            result = self.service.process_spec(spec_input)
            
            return {
                'status': 'success' if result.success else 'error',
                'message': result.message,
                'data': result.data,
                'spec_info': {
                    'format': spec_input.format,
                    'length': len(spec_input.content),
                    'created_at': spec_input.created_at.isoformat()
                }
            }
            
        except Exception as e:
            self.logger.error(f"Error processing specification: {str(e)}")
            return {
                'status': 'error',
                'message': f'Processing failed: {str(e)}'
            }
    
    def _get_validation_errors(self, spec_input: SpecInput) -> list:
        """Get validation errors for a spec input."""
        errors = []
        
        if len(spec_input.content.strip()) < SpecInput.MIN_CONTENT_LENGTH:
            errors.append(f"Content too short (minimum {SpecInput.MIN_CONTENT_LENGTH} characters)")
            
        if spec_input.format not in SpecInput.VALID_FORMATS:
            errors.append(f"Invalid format '{spec_input.format}'. Valid formats: {', '.join(SpecInput.VALID_FORMATS)}")
            
        return errors
    
    def get_application_status(self) -> Dict[str, Any]:
        """
        Get the current status of the spec input application.
        
        Returns:
            Dict[str, Any]: Application status information
        """
        return {
            'status': 'running',
            'processed_specs': self.service.get_processed_count(),
            'supported_formats': list(SpecInput.VALID_FORMATS),
            'min_content_length': SpecInput.MIN_CONTENT_LENGTH
        }


def main():
    """
    Main entry point for the spec input application.
    
    This function demonstrates the basic usage of the spec input functionality.
    """
    app = SpecInputApp()
    
    print("NorthstarET LMS - Spec Input Module")
    print("=" * 40)
    
    # Display application status
    status = app.get_application_status()
    print(f"Status: {status['status']}")
    print(f"Supported formats: {', '.join(status['supported_formats'])}")
    print(f"Minimum content length: {status['min_content_length']}")
    print()
    
    # Example processing
    test_specs = [
        {
            'content': "This is a basic specification for testing the system functionality.",
            'format': 'text'
        },
        {
            'content': "# Test Specification\n\nThis is a **markdown** specification with formatting.",
            'format': 'markdown'
        },
        {
            'content': "x",  # This should fail validation
            'format': 'text'
        }
    ]
    
    for i, spec in enumerate(test_specs, 1):
        print(f"Processing test specification {i}...")
        result = app.process_specification(spec['content'], spec['format'])
        print(f"  Status: {result['status']}")
        print(f"  Message: {result['message']}")
        if 'errors' in result:
            print(f"  Errors: {result['errors']}")
        print()
    
    # Final status
    final_status = app.get_application_status()
    print(f"Final processed count: {final_status['processed_specs']}")


if __name__ == "__main__":
    main()