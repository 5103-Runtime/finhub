import {
  useId,
  useRef,
  useState,
  type ChangeEvent,
  type DragEvent,
  type KeyboardEvent,
} from "react";
import { Box, IconButton, Typography } from "@mui/material";
import CloseRounded from "@mui/icons-material/CloseRounded";
import DescriptionRounded from "@mui/icons-material/DescriptionRounded";
import UploadFileRounded from "@mui/icons-material/UploadFileRounded";
import { FormAlert } from "@app/components/Form/FormAlert";
import { formatFileSize, validateStatementFile } from "@app/utils/statements";

import "./StatementUpload.css";

type StatementUploadProps = {
  // The chosen file, held by the parent. It is always a valid CSV or null.
  file: File | null;
  onFileChange: (file: File | null) => void;
  disabled?: boolean;
};

/**
 * @brief Drag and drop area for choosing one CSV bank statement. Clicking it or
 * pressing Enter or Space opens the file picker instead. Invalid files are
 * rejected here with a message, so the parent only ever receives a valid file.
 */
export default function StatementUpload({
  file,
  onFileChange,
  disabled = false,
}: StatementUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const errorId = useId();
  const [isDragging, setIsDragging] = useState(false);
  const [error, setError] = useState("");

  const chooseFiles = (files: FileList | null) => {
    if (!files || files.length === 0) {
      return;
    }

    if (files.length > 1) {
      setError("Upload one file at a time.");
      onFileChange(null);
      return;
    }

    const selectedFile = files[0];
    const validationError = validateStatementFile(selectedFile);
    if (validationError) {
      setError(validationError);
      onFileChange(null);
      return;
    }

    setError("");
    onFileChange(selectedFile);
  };

  const openPicker = () => {
    if (!disabled) {
      inputRef.current?.click();
    }
  };

  const handleInputChange = (event: ChangeEvent<HTMLInputElement>) => {
    chooseFiles(event.target.files);
    // Clear the input so choosing the same file again still fires onChange.
    event.target.value = "";
  };

  const handleDragOver = (event: DragEvent<HTMLDivElement>) => {
    // Without preventDefault the browser opens the dropped file instead.
    event.preventDefault();
    if (!disabled) {
      setIsDragging(true);
    }
  };

  const handleDragLeave = (event: DragEvent<HTMLDivElement>) => {
    // Moving over a child element also fires dragleave, so ignore that case.
    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
      setIsDragging(false);
    }
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
    if (!disabled) {
      chooseFiles(event.dataTransfer.files);
    }
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      openPicker();
    }
  };

  const removeFile = () => {
    setError("");
    onFileChange(null);
  };

  const zoneClass = [
    "statement-upload-zone",
    isDragging && "dragging",
    error && "invalid",
    disabled && "disabled",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <Box className="statement-upload">
      <Box
        className={zoneClass}
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-disabled={disabled}
        aria-label="Upload a CSV statement. Drag a file here, or press Enter to browse."
        aria-describedby={error ? errorId : undefined}
        onClick={openPicker}
        onKeyDown={handleKeyDown}
        onDragOver={handleDragOver}
        onDragEnter={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        <UploadFileRounded className="statement-upload-icon" />
        <Typography className="statement-upload-title">
          {isDragging ? "Drop your file here" : "Drag and drop your statement"}
        </Typography>
        <Typography className="statement-upload-hint">
          or <span className="statement-upload-browse">browse</span> to choose a
          CSV file
        </Typography>
        <input
          ref={inputRef}
          type="file"
          accept=".csv,text/csv"
          hidden
          onClick={(event)=> event.stopPropagation()}
          onChange={handleInputChange}
        />
      </Box>

      {error && (
        <Box id={errorId} className="statement-upload-error">
          <FormAlert severity="error">{error}</FormAlert>
        </Box>
      )}

      {file && (
        <Box className="statement-upload-file">
          <DescriptionRounded className="statement-upload-file-icon" />
          <Box className="statement-upload-file-info">
            <Typography className="statement-upload-file-name" title={file.name}>
              {file.name}
            </Typography>
            <Typography className="statement-upload-file-size">
              {formatFileSize(file.size)}
            </Typography>
          </Box>
          <IconButton
            className="statement-upload-remove"
            aria-label={`Remove ${file.name}`}
            onClick={removeFile}
            disabled={disabled}
          >
            <CloseRounded />
          </IconButton>
        </Box>
      )}
    </Box>
  );
}