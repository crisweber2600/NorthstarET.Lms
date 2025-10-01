"""
SpecInput model for handling specification input data.

This module defines the SpecInput class that represents and validates
specification input data in the NorthstarET LMS system.
"""

from typing import Optional, Dict, Any
import re
from datetime import datetime, timezone


class SpecInput:
    """
    Model for specification input data.
    
    This class handles the creation, validation, and sanitization
    of specification input data.
    """
    
    VALID_FORMATS = {"markdown", "text", "html"}
    MIN_CONTENT_LENGTH = 2
    
    def __init__(self, content: str, format: str = "text", metadata: Optional[Dict[str, Any]] = None):
        """
        Initialize a SpecInput instance.
        
        Args:
            content (str): The specification content
            format (str): The format of the content (markdown, text, html)
            metadata (Dict): Optional metadata for the specification
        """
        self.content = content
        self.format = format.lower() if format else "text"
        self.metadata = metadata or {}
        self.created_at = datetime.now(timezone.utc)
        self._sanitized_content = None
        
    def is_valid(self) -> bool:
        """
        Validate the specification input.
        
        Returns:
            bool: True if the specification is valid, False otherwise
        """
        # Check content length
        if len(self.content.strip()) < self.MIN_CONTENT_LENGTH:
            return False
            
        # Check format
        if self.format not in self.VALID_FORMATS:
            return False
            
        return True
        
    def sanitize(self) -> str:
        """
        Sanitize the specification content.
        
        Returns:
            str: Sanitized content with potentially harmful content removed
        """
        if self._sanitized_content is not None:
            return self._sanitized_content
            
        content = self.content
        
        # Remove script tags and their content
        content = re.sub(r'<script[^>]*>.*?</script>', '', content, flags=re.DOTALL | re.IGNORECASE)
        
        # Remove other potentially harmful HTML tags
        harmful_tags = ['iframe', 'object', 'embed', 'form', 'input']
        for tag in harmful_tags:
            content = re.sub(f'<{tag}[^>]*>.*?</{tag}>', '', content, flags=re.DOTALL | re.IGNORECASE)
            content = re.sub(f'<{tag}[^>]*/?>', '', content, flags=re.IGNORECASE)
            
        self._sanitized_content = content.strip()
        return self._sanitized_content
        
    def to_dict(self) -> Dict[str, Any]:
        """
        Convert the SpecInput to a dictionary representation.
        
        Returns:
            Dict: Dictionary representation of the SpecInput
        """
        return {
            'content': self.content,
            'format': self.format,
            'metadata': self.metadata,
            'created_at': self.created_at.isoformat(),
            'is_valid': self.is_valid()
        }
        
    def __str__(self) -> str:
        """String representation of the SpecInput."""
        return f"SpecInput(format={self.format}, length={len(self.content)}, valid={self.is_valid()})"
        
    def __repr__(self) -> str:
        """Detailed representation of the SpecInput."""
        return f"SpecInput(content='{self.content[:50]}...', format='{self.format}', valid={self.is_valid()})"