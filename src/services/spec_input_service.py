"""
SpecInputService for processing specification input data.

This module provides business logic for processing and managing
specification input in the NorthstarET LMS system.
"""

from typing import Dict, Any, Optional, List
import logging
from datetime import datetime, timezone

from models.spec_input import SpecInput


class SpecInputProcessingResult:
    """Result object for spec input processing operations."""
    
    def __init__(self, success: bool, message: str, data: Optional[Dict[str, Any]] = None):
        """
        Initialize a processing result.
        
        Args:
            success (bool): Whether the operation was successful
            message (str): Description of the result
            data (Dict): Optional additional data
        """
        self.success = success
        self.message = message
        self.data = data or {}
        self.timestamp = datetime.now(timezone.utc)
        
    def to_dict(self) -> Dict[str, Any]:
        """Convert result to dictionary."""
        return {
            'success': self.success,
            'message': self.message,
            'data': self.data,
            'timestamp': self.timestamp.isoformat()
        }


class SpecInputService:
    """
    Service class for handling specification input operations.
    
    This service provides methods for processing, validating, and managing
    specification input data.
    """
    
    def __init__(self, logger: Optional[logging.Logger] = None):
        """
        Initialize the SpecInputService.
        
        Args:
            logger (logging.Logger): Optional logger instance
        """
        self.logger = logger or logging.getLogger(__name__)
        self._processed_specs: List[SpecInput] = []
        
    def process_spec(self, spec_input: SpecInput) -> SpecInputProcessingResult:
        """
        Process a specification input.
        
        Args:
            spec_input (SpecInput): The specification to process
            
        Returns:
            SpecInputProcessingResult: Result of the processing operation
            
        Raises:
            ValueError: If the specification is invalid
        """
        if not isinstance(spec_input, SpecInput):
            return SpecInputProcessingResult(
                success=False,
                message="Invalid input: must be SpecInput instance"
            )
            
        # Validate the specification
        if not spec_input.is_valid():
            raise ValueError(f"Invalid specification: {spec_input}")
            
        try:
            # Sanitize the content
            sanitized_content = spec_input.sanitize()
            
            # Process the specification
            processed_data = {
                'original_length': len(spec_input.content),
                'sanitized_length': len(sanitized_content),
                'format': spec_input.format,
                'created_at': spec_input.created_at.isoformat(),
                'processed_at': datetime.now(timezone.utc).isoformat()
            }
            
            # Store the processed spec
            self._processed_specs.append(spec_input)
            
            self.logger.info(f"Successfully processed spec: {spec_input}")
            
            return SpecInputProcessingResult(
                success=True,
                message="Specification processed successfully",
                data=processed_data
            )
            
        except Exception as e:
            error_msg = f"Error processing specification: {str(e)}"
            self.logger.error(error_msg)
            return SpecInputProcessingResult(
                success=False,
                message=error_msg
            )
            
    def handle_invalid_spec(self, spec_input: Optional[SpecInput]) -> SpecInputProcessingResult:
        """
        Handle invalid specification input gracefully.
        
        Args:
            spec_input (SpecInput): The specification to handle (can be None)
            
        Returns:
            SpecInputProcessingResult: Result of the handling operation
        """
        if spec_input is None:
            return SpecInputProcessingResult(
                success=False,
                message="Cannot process null specification"
            )
            
        return SpecInputProcessingResult(
            success=False,
            message=f"Invalid specification: {spec_input}",
            data={
                'errors': self._get_validation_errors(spec_input)
            }
        )
        
    def _get_validation_errors(self, spec_input: SpecInput) -> List[str]:
        """
        Get list of validation errors for a specification.
        
        Args:
            spec_input (SpecInput): The specification to validate
            
        Returns:
            List[str]: List of validation error messages
        """
        errors = []
        
        if len(spec_input.content.strip()) < SpecInput.MIN_CONTENT_LENGTH:
            errors.append(f"Content too short (minimum {SpecInput.MIN_CONTENT_LENGTH} characters)")
            
        if spec_input.format not in SpecInput.VALID_FORMATS:
            errors.append(f"Invalid format '{spec_input.format}'. Valid formats: {', '.join(SpecInput.VALID_FORMATS)}")
            
        return errors
        
    def get_processed_count(self) -> int:
        """Get the number of processed specifications."""
        return len(self._processed_specs)
        
    def get_processed_specs(self) -> List[SpecInput]:
        """Get all processed specifications."""
        return self._processed_specs.copy()
        
    def clear_processed_specs(self) -> None:
        """Clear all processed specifications."""
        self._processed_specs.clear()
        self.logger.info("Cleared all processed specifications")